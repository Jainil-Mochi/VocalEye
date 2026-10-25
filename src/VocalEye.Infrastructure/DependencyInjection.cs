using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Domain.Conversations;
using VocalEye.Infrastructure.Llm;
using VocalEye.Infrastructure.Persistence;

namespace VocalEye.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the model catalog and the runtime-switchable chat client factory.</summary>
    /// <exception cref="InvalidOperationException">No providers are defined in configuration.</exception>
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection providers = configuration.GetSection($"{LlmOptions.Section}:Providers");
        if (!providers.GetChildren().Any())
        {
            throw new InvalidOperationException(
                "Configuration section 'Llm:Providers' is missing or empty. Define at least one provider (see appsettings.json).");
        }

        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.Section));
        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        return services;
    }

    /// <summary>Registers conversation storage: Postgres (default), Sqlite or InMemory, chosen by Persistence:Provider.</summary>
    /// <exception cref="InvalidOperationException">Unknown provider, or Postgres selected without a connection string.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string provider = PersistenceProviders.Resolve(configuration);

        if (string.Equals(provider, PersistenceProviders.InMemory, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<InMemoryConversationRepository>();
            services.AddSingleton<IConversationRepository>(serviceProvider => serviceProvider.GetRequiredService<InMemoryConversationRepository>());
            services.AddSingleton<IConversationQueries, InMemoryConversationQueries>();
            services.AddScoped<IConversationStore, ConversationStore>();
            services.AddScoped<IConversationManager, ConversationManager>();
            return services;
        }

        if (string.Equals(provider, PersistenceProviders.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            // No setup needed for a desktop user: without a connection string the file goes to %LocalAppData%\VocalEye.
            string sqliteConnection = configuration.GetConnectionString("Sqlite") ?? SqliteConnectionStrings.CreateDefault();
            SqliteConnectionStrings.EnsureDirectoryExists(sqliteConnection);

            services.AddDbContext<VocalEyeDbContext>(options => options.UseSqlite(sqliteConnection));
            AddDatabaseServices(services);
            return services;
        }

        if (!string.Equals(provider, PersistenceProviders.Postgres, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Persistence:Provider '{provider}' is not supported. Use 'Postgres', 'Sqlite' or 'InMemory'.");
        }

        string? connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not configured. Add to appsettings.json: " +
                "\"Persistence\": { \"Provider\": \"Postgres\" }, " +
                "\"ConnectionStrings\": { \"Postgres\": \"Host=localhost;Port=5432;Database=vocaleye;Username=vocaleye;Password=vocaleye\" } " +
                "(matches docker-compose.yml), or set Persistence:Provider to Sqlite or InMemory.");
        }

        services.AddDbContext<VocalEyeDbContext>(options => options.UseNpgsql(connectionString));
        AddDatabaseServices(services);
        return services;
    }

    private static void AddDatabaseServices(IServiceCollection services)
    {
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationQueries, ConversationQueries>();
        services.AddScoped<IConversationStore, ConversationStore>();
        services.AddScoped<IConversationManager, ConversationManager>();
    }
}

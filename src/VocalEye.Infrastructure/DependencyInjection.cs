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

    /// <summary>Registers conversation storage: Postgres (default) or InMemory, chosen by Persistence:Provider.</summary>
    /// <exception cref="InvalidOperationException">Unknown provider, or Postgres selected without a connection string.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string provider = configuration["Persistence:Provider"] ?? "Postgres";

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<InMemoryConversationRepository>();
            services.AddSingleton<IConversationRepository>(serviceProvider => serviceProvider.GetRequiredService<InMemoryConversationRepository>());
            services.AddSingleton<IConversationQueries, InMemoryConversationQueries>();
            services.AddScoped<IConversationStore, ConversationStore>();
            return services;
        }

        if (!string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Persistence:Provider '{provider}' is not supported. Use 'Postgres' or 'InMemory'.");
        }

        string? connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Postgres is not configured. Add to appsettings.json: " +
                "\"Persistence\": { \"Provider\": \"Postgres\" }, " +
                "\"ConnectionStrings\": { \"Postgres\": \"Host=localhost;Port=5432;Database=vocaleye;Username=vocaleye;Password=vocaleye\" } " +
                "(matches docker-compose.yml), or set Persistence:Provider to InMemory.");
        }

        services.AddDbContext<VocalEyeDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationQueries, ConversationQueries>();
        services.AddScoped<IConversationStore, ConversationStore>();
        return services;
    }
}

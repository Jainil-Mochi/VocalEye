using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
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

    /// <summary>Development conversation store (lost on restart). Replaced by Postgres in a later slice.</summary>
    public static IServiceCollection AddInMemoryConversationStore(this IServiceCollection services)
    {
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        return services;
    }
}

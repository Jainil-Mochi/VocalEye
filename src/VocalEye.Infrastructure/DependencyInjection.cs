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
    public static IServiceCollection AddLlm(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetSection(LlmOptions.Section))
            .Validate(o => o.Providers.Count > 0, "Llm:Providers must define at least one provider.")
            .ValidateOnStart();

        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        return services;
    }

    /// <summary>Development conversation store. Swap for the Postgres one later.</summary>
    public static IServiceCollection AddInMemoryConversationStore(this IServiceCollection services)
        => services.AddSingleton<IConversationStore, InMemoryConversationStore>();
}

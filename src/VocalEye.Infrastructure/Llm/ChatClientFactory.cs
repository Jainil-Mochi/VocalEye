using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Infrastructure.Llm.Providers;

namespace VocalEye.Infrastructure.Llm;

public sealed class ChatClientFactory(
    IOptions<LlmOptions> options,
    IModelCatalog catalog,
    ILoggerFactory loggerFactory) : IChatClientFactory
{
    public const string TelemetrySource = "VocalEye.Llm";

    private readonly ConcurrentDictionary<(string Provider, string Model), IChatClient> _cache = new();

    public IChatClient Create(ModelSelection selection)
    {
        if (catalog.Find(selection) is null)
            throw new ArgumentException(
                $"Model '{selection.Provider}/{selection.Model}' is not available. Check Llm:Providers in configuration and that its API key/endpoint is set.",
                nameof(selection));

        var key = (selection.Provider.ToLowerInvariant(), selection.Model.ToLowerInvariant());
        return _cache.GetOrAdd(key, _ => Build(selection));
    }

    private IChatClient Build(ModelSelection selection)
    {
        var provider = options.Value.Providers
            .First(p => string.Equals(p.Key, selection.Provider, StringComparison.OrdinalIgnoreCase)).Value;

        return ProviderClientBuilder.Build(provider, selection.Model)
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, TelemetrySource)
            .Build();
    }
}

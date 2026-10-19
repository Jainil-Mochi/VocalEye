using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Infrastructure.Llm.Providers;

namespace VocalEye.Infrastructure.Llm;

public sealed class ChatClientFactory : IChatClientFactory
{
    public const string TelemetrySource = "VocalEye.Llm";

    private readonly IOptions<LlmOptions> _options;
    private readonly IModelCatalog _catalog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<(string Provider, string Model), IChatClient> _cache = new ConcurrentDictionary<(string Provider, string Model), IChatClient>();

    public ChatClientFactory(IOptions<LlmOptions> options, IModelCatalog catalog, ILoggerFactory loggerFactory)
    {
        _options = options;
        _catalog = catalog;
        _loggerFactory = loggerFactory;
    }

    public IChatClient Create(ModelSelection selection)
    {
        if (_catalog.Find(selection) is null)
        {
            throw new ArgumentException(
                $"Model '{selection.Provider}/{selection.Model}' is not available. Check Llm:Providers in configuration and that its API key/endpoint is set.",
                nameof(selection));
        }

        (string, string) key = (selection.Provider.ToLowerInvariant(), selection.Model.ToLowerInvariant());
        return _cache.GetOrAdd(key, _ => Build(selection));
    }

    private IChatClient Build(ModelSelection selection)
    {
        ProviderOptions provider = _options.Value.Providers
            .First(entry => string.Equals(entry.Key, selection.Provider, StringComparison.OrdinalIgnoreCase))
            .Value;

        IChatClient raw = ProviderClientBuilder.Build(provider, selection.Model);

        return new ErrorDetailChatClient(raw, $"{selection.Provider}/{selection.Model}")
            .AsBuilder()
            .UseOpenTelemetry(_loggerFactory, TelemetrySource)
            .Build();
    }
}

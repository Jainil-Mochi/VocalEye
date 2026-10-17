using Microsoft.Extensions.Options;
using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Infrastructure.Llm;

public sealed class ModelCatalog : IModelCatalog
{
    private readonly IReadOnlyList<ModelInfo> _models;

    public ModelCatalog(IOptions<LlmOptions> options)
    {
        var o = options.Value;

        _models = o.Providers
            .Where(p => p.Value.IsConfigured)
            .SelectMany(p => p.Value.Models.Select(m => new ModelInfo(
                Provider: p.Key.ToLowerInvariant(),
                Model: m.Id,
                DisplayName: m.DisplayName ?? m.Id,
                SupportsTools: m.SupportsTools,
                SupportsStreaming: m.SupportsStreaming,
                SupportsVision: m.SupportsVision,
                ContextWindowTokens: m.ContextWindowTokens)))
            .ToList();

        var configuredDefault = new ModelSelection(o.Default.Provider.ToLowerInvariant(), o.Default.Model);
        Default = Find(configuredDefault) is not null || _models.Count == 0
            ? configuredDefault
            : new ModelSelection(_models[0].Provider, _models[0].Model); // configured default has no key: use first available
    }

    public ModelSelection Default { get; }

    public IReadOnlyList<ModelInfo> List() => _models;

    public ModelInfo? Find(ModelSelection selection) => _models.FirstOrDefault(m =>
        string.Equals(m.Provider, selection.Provider, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(m.Model, selection.Model, StringComparison.OrdinalIgnoreCase));
}

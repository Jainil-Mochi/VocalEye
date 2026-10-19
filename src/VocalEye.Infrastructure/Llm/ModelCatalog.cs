using Microsoft.Extensions.Options;
using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Infrastructure.Llm;

public sealed class ModelCatalog : IModelCatalog
{
    private readonly IReadOnlyList<ModelInfo> _models;

    public ModelCatalog(IOptions<LlmOptions> options)
    {
        LlmOptions llmOptions = options.Value;

        _models = llmOptions.Providers
            .Where(provider => provider.Value.IsConfigured)
            .SelectMany(provider => provider.Value.Models.Select(model => new ModelInfo(
                Provider: provider.Key.ToLowerInvariant(),
                Model: model.Id,
                DisplayName: model.DisplayName ?? model.Id,
                SupportsTools: model.SupportsTools,
                SupportsStreaming: model.SupportsStreaming,
                SupportsVision: model.SupportsVision,
                ContextWindowTokens: model.ContextWindowTokens)))
            .ToList();

        ModelSelection configuredDefault = new ModelSelection(
            llmOptions.Default.Provider.ToLowerInvariant(),
            llmOptions.Default.Model);

        // A configured default without credentials is skipped: fall back to the first model that can actually be called.
        Default = Find(configuredDefault) is not null || _models.Count == 0
            ? configuredDefault
            : new ModelSelection(_models[0].Provider, _models[0].Model);
    }

    public ModelSelection Default { get; }

    public IReadOnlyList<ModelInfo> List()
    {
        return _models;
    }

    public ModelInfo? Find(ModelSelection selection)
    {
        return _models.FirstOrDefault(model =>
            string.Equals(model.Provider, selection.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(model.Model, selection.Model, StringComparison.OrdinalIgnoreCase));
    }
}

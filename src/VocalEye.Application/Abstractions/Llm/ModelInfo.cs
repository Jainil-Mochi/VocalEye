namespace VocalEye.Application.Abstractions.Llm;

/// <summary>What the catalog knows about a model (from configuration), so the UI can list it and the agent can adapt.</summary>
public sealed record ModelInfo(
    string Provider,
    string Model,
    string DisplayName,
    bool SupportsTools = true,
    bool SupportsStreaming = true,
    bool SupportsVision = false,
    int? ContextWindowTokens = null);

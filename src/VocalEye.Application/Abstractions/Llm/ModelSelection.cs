namespace VocalEye.Application.Abstractions.Llm;

/// <summary>Which model to use for a request. Chosen by the user at runtime; falls back to the configured default.</summary>
public sealed record ModelSelection(string Provider, string Model);

/// <summary>What the catalog knows about a model (from configuration), so the UI can list it and the agent can adapt.</summary>
public sealed record ModelInfo(
    string Provider,
    string Model,
    string DisplayName,
    bool SupportsTools = true,
    bool SupportsStreaming = true,
    bool SupportsVision = false,
    int? ContextWindowTokens = null);

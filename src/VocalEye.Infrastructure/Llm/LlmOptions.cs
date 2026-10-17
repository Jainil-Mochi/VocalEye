namespace VocalEye.Infrastructure.Llm;

/// <summary>Bound from the "Llm" config section. See appsettings.json for the shape.</summary>
public sealed class LlmOptions
{
    public const string Section = "Llm";

    public DefaultModelOptions Default { get; set; } = new();

    /// <summary>Key = provider name shown to clients (e.g. "gemini", "openai", "ollama").</summary>
    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DefaultModelOptions
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
}

public sealed class ProviderOptions
{
    /// <summary>OpenAI | AzureOpenAI | OpenAICompatible | Ollama</summary>
    public string Type { get; set; } = "";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public List<ModelOptions> Models { get; set; } = [];

    /// <summary>A provider is offered to users only when it has what it needs to be called.</summary>
    public bool IsConfigured => Type.ToLowerInvariant() switch
    {
        "ollama" => !string.IsNullOrWhiteSpace(Endpoint),
        "azureopenai" => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey),
        "openai" or "openaicompatible" => !string.IsNullOrWhiteSpace(ApiKey),
        _ => false,
    };
}

public sealed class ModelOptions
{
    public string Id { get; set; } = "";
    public string? DisplayName { get; set; }
    public bool SupportsTools { get; set; } = true;
    public bool SupportsStreaming { get; set; } = true;
    public bool SupportsVision { get; set; }
    public int? ContextWindowTokens { get; set; }
}

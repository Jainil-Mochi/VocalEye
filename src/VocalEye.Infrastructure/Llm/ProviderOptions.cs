namespace VocalEye.Infrastructure.Llm;

public sealed class ProviderOptions
{
    /// <summary>OpenAI | AzureOpenAI | OpenAICompatible | Ollama</summary>
    public string Type { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Give up on a single call to the model after this many seconds.</summary>
    public int RequestTimeoutSeconds { get; set; } = 60;

    /// <summary>How many times to retry a failed call (overload, rate limit, timeout). 0 turns retries off.</summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    /// Gemini 3 only: signature sent for tool calls whose real "thought signature" was lost. Leave unset for the default
    /// (skip_thought_signature_validator); set to an empty string to switch the workaround off.
    /// </summary>
    public string? ThoughtSignatureFallback { get; set; }

    public List<ModelOptions> Models { get; set; } = new List<ModelOptions>();

    /// <summary>A provider is offered to users only when it has what it needs to be called.</summary>
    public bool IsConfigured => Type.ToLowerInvariant() switch
    {
        "ollama" => !string.IsNullOrWhiteSpace(Endpoint),
        "azureopenai" => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey),
        "openai" or "openaicompatible" => !string.IsNullOrWhiteSpace(ApiKey),
        _ => false,
    };
}

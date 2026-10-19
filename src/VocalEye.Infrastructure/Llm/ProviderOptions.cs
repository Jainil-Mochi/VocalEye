namespace VocalEye.Infrastructure.Llm;

public sealed class ProviderOptions
{
    /// <summary>OpenAI | AzureOpenAI | OpenAICompatible | Ollama</summary>
    public string Type { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

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

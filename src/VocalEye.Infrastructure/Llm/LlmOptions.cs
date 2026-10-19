namespace VocalEye.Infrastructure.Llm;

/// <summary>Bound from the "Llm" config section. See appsettings.json for the shape.</summary>
public sealed class LlmOptions
{
    public const string Section = "Llm";

    public DefaultModelOptions Default { get; set; } = new DefaultModelOptions();

    /// <summary>Key = provider name shown to clients (for example "gemini", "openai", "groq").</summary>
    public Dictionary<string, ProviderOptions> Providers { get; set; } = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase);
}

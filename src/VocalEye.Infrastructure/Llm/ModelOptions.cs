namespace VocalEye.Infrastructure.Llm;

public sealed class ModelOptions
{
    public string Id { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool SupportsTools { get; set; } = true;

    public bool SupportsStreaming { get; set; } = true;

    public bool SupportsVision { get; set; }

    public int? ContextWindowTokens { get; set; }
}

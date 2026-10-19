namespace VocalEye.Contracts.Chat;

/// <summary>Item returned by GET /api/models (drives the model picker in every client).</summary>
public class ModelDto
{
    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool SupportsTools { get; set; }

    public bool SupportsStreaming { get; set; }

    public bool SupportsVision { get; set; }

    public bool IsDefault { get; set; }
}

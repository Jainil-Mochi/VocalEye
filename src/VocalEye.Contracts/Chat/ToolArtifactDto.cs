namespace VocalEye.Contracts.Chat;

/// <summary>Something a tool produced that the client can offer to open.</summary>
public class ToolArtifactDto
{
    /// <summary>"file", "folder" or "url".</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The full path, or the web address.</summary>
    public string Value { get; set; } = string.Empty;
}

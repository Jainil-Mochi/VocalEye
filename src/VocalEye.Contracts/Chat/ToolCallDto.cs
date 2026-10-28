namespace VocalEye.Contracts.Chat;

public class ToolCallDto
{
    public string Name { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = "{}";

    public bool Success { get; set; }

    public string Output { get; set; } = string.Empty;

    /// <summary>Files, folders or links the tool produced; empty for most tools.</summary>
    public List<ToolArtifactDto> Artifacts { get; set; } = new List<ToolArtifactDto>();
}

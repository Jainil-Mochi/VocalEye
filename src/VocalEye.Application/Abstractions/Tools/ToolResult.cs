namespace VocalEye.Application.Abstractions.Tools;

public sealed record ToolResult(bool Success, string Output, string? Error = null, IReadOnlyList<ToolArtifact>? Artifacts = null)
{
    public static ToolResult Ok(string output, IReadOnlyList<ToolArtifact>? artifacts = null)
    {
        return new ToolResult(true, output, null, artifacts);
    }

    public static ToolResult Fail(string error)
    {
        return new ToolResult(false, string.Empty, error);
    }
}

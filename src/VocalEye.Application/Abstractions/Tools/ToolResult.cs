namespace VocalEye.Application.Abstractions.Tools;

public sealed record ToolResult(bool Success, string Output, string? Error = null)
{
    public static ToolResult Ok(string output)
    {
        return new ToolResult(true, output);
    }

    public static ToolResult Fail(string error)
    {
        return new ToolResult(false, string.Empty, error);
    }
}

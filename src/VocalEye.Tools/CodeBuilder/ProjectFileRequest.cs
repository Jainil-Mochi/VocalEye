namespace VocalEye.Tools.CodeBuilder;

/// <summary>One file of a project as sent by the model, after its path has been checked.</summary>
internal sealed class ProjectFileRequest
{
    /// <summary>Normalized relative path using forward slashes, for example "src/app.py".</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Full path on disk, proven to be inside the project folder.</summary>
    public string FullPath { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

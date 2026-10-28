using System.Text;
using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Common;

namespace VocalEye.Tools.CodeBuilder;

/// <summary>
/// Saves a project the agent has written (a set of files) into a new folder. Port of the Python code_builder.
/// The agent's model writes every file; this tool only validates and saves. It never runs anything: the Python
/// version executed the generated project's run command, which is how a bad or injected answer would run code
/// on your machine (ADR-0010).
/// </summary>
public sealed class CodeBuilderTool : IAgentTool
{
    private const int MaximumFiles = 60;
    private const int MaximumFileBytes = 256 * 1024;
    private const int MaximumTotalBytes = 2 * 1024 * 1024;
    private const string SubFolder = "projects";

    private readonly ToolOutputOptions _options;

    public CodeBuilderTool(ToolOutputOptions options)
    {
        _options = options;
    }

    public string Name => "code_builder";

    public string Description =>
        "Create a software project (source files, README, config) in a new folder on this computer. " +
        "You must write every file completely yourself and pass them all in 'files'. Nothing is run.";

    public ToolRiskLevel Risk => ToolRiskLevel.Write;

    public string ParametersJsonSchema => """
        {"type":"object","properties":{
            "projectName":{"type":"string","description":"Short folder name for the project, for example 'todo-api'."},
            "files":{"type":"array","description":"Every file of the project with its complete content. Include a README.md that explains how to run it.","items":{
                "type":"object","properties":{
                    "path":{"type":"string","description":"Relative path inside the project, for example 'src/app.py'."},
                    "content":{"type":"string","description":"The complete file content. Use an empty string for an empty file."}
                },"required":["path","content"]}}
        },"required":["projectName","files"]}
        """;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string projectName = ToolArguments.GetRequiredString(arguments, "projectName");
        JsonElement filesElement = ToolArguments.GetRequiredArray(arguments, "files");

        int fileCount = filesElement.GetArrayLength();
        if (fileCount == 0)
        {
            return ToolResult.Fail("The 'files' list is empty. Send every file of the project.");
        }

        if (fileCount > MaximumFiles)
        {
            return ToolResult.Fail($"Too many files ({fileCount}). The limit is {MaximumFiles}; make the project smaller.");
        }

        string projectsRoot = Path.Combine(_options.OutputDirectory, SubFolder);
        string projectDirectory = SafePath.MakeUniquePath(Path.Combine(projectsRoot, SafePath.SanitizeName(projectName, "project")));

        // Validate everything first so a bad file never leaves a half-written project behind.
        List<ProjectFileRequest> files = new List<ProjectFileRequest>();
        int totalBytes = 0;

        foreach (JsonElement item in filesElement.EnumerateArray())
        {
            string relativePath = ToolArguments.GetRequiredString(item, "path");
            string? content = ToolArguments.GetOptionalString(item, "content");
            if (content is null)
            {
                return ToolResult.Fail($"File '{relativePath}' has no 'content'. Use an empty string for an empty file.");
            }

            int bytes = Encoding.UTF8.GetByteCount(content);
            if (bytes > MaximumFileBytes)
            {
                return ToolResult.Fail($"File '{relativePath}' is too large ({bytes} bytes). The limit is {MaximumFileBytes} bytes per file.");
            }

            totalBytes += bytes;
            if (totalBytes > MaximumTotalBytes)
            {
                return ToolResult.Fail($"The project is too large. The limit is {MaximumTotalBytes} bytes in total.");
            }

            files.Add(new ProjectFileRequest
            {
                RelativePath = relativePath.Trim().Replace('\\', '/'),
                FullPath = SafePath.Resolve(projectDirectory, relativePath),
                Content = content,
            });
        }

        string? layoutProblem = FindLayoutProblem(files);
        if (layoutProblem is not null)
        {
            return ToolResult.Fail(layoutProblem);
        }

        foreach (ProjectFileRequest file in files)
        {
            string? directory = Path.GetDirectoryName(file.FullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(file.FullPath, file.Content, new UTF8Encoding(false), cancellationToken);
        }

        return ToolResult.Ok(
            BuildSummary(projectDirectory, files),
            new[] { new ToolArtifact(ToolArtifactKinds.Folder, projectDirectory) });
    }

    /// <summary>Finds duplicate paths and a path used as both a file and a folder ("a" and "a/b").</summary>
    private static string? FindLayoutProblem(IReadOnlyList<ProjectFileRequest> files)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ProjectFileRequest file in files)
        {
            if (!seen.Add(file.FullPath))
            {
                return $"The path '{file.RelativePath}' appears more than once. Each file may be sent only once.";
            }
        }

        foreach (ProjectFileRequest file in files)
        {
            string asFolder = file.FullPath + Path.DirectorySeparatorChar;
            ProjectFileRequest? clash = files.FirstOrDefault(other => other.FullPath.StartsWith(asFolder, StringComparison.OrdinalIgnoreCase));
            if (clash is not null)
            {
                return $"'{file.RelativePath}' is used as a file, but '{clash.RelativePath}' needs it to be a folder.";
            }
        }

        return null;
    }

    private static string BuildSummary(string projectDirectory, IReadOnlyList<ProjectFileRequest> files)
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine($"Created the project in: {projectDirectory}");
        summary.AppendLine($"Files ({files.Count}):");

        foreach (ProjectFileRequest file in files.OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            summary.AppendLine($"- {file.RelativePath}");
        }

        summary.Append("Nothing was run. The user can open the folder and follow the README to run it.");
        return summary.ToString();
    }
}

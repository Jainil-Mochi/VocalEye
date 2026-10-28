using System.Text;
using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Common;
using VocalEye.Tools.Documents;

namespace VocalEye.Tools.FileGeneration;

/// <summary>
/// Saves a document the agent has written as txt, md, pdf or docx. Port of the Python file_generator, with one change:
/// the agent's model writes the content and passes it in, instead of the tool calling a second model itself.
/// </summary>
public sealed class FileGenerationTool : IAgentTool
{
    private const int MaximumContentLength = 200_000;
    private const string SubFolder = "documents";

    private readonly ToolOutputOptions _options;
    private readonly Dictionary<string, IDocumentWriter> _writers;

    public FileGenerationTool(ToolOutputOptions options)
    {
        _options = options;
        _writers = new IDocumentWriter[] { new PdfDocumentWriter(), new DocxDocumentWriter() }
            .ToDictionary(writer => writer.Extension, StringComparer.OrdinalIgnoreCase);
    }

    public string Name => "file_generator";

    public string Description =>
        "Create a document file (txt, md, pdf or docx) and save it on this computer. " +
        "You must write the complete document text yourself and pass it in 'content'.";

    public ToolRiskLevel Risk => ToolRiskLevel.Write;

    public string ParametersJsonSchema => """
        {"type":"object","properties":{
            "filename":{"type":"string","description":"File name without extension, for example 'ai-trends-report'."},
            "filetype":{"type":"string","enum":["txt","md","pdf","docx"],"description":"Output format."},
            "content":{"type":"string","description":"The full document text. Use Markdown: '# ' title, '## ' and '### ' headings, '- ' bullets, **bold**, *italic*. Write the whole document; do not abbreviate or leave placeholders."}
        },"required":["filename","filetype","content"]}
        """;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string filename = ToolArguments.GetRequiredString(arguments, "filename");
        string fileType = ToolArguments.GetRequiredString(arguments, "filetype").Trim().TrimStart('.').ToLowerInvariant();
        string content = ToolArguments.GetRequiredString(arguments, "content");

        if (content.Length > MaximumContentLength)
        {
            return ToolResult.Fail($"The content is too long ({content.Length} characters). The limit is {MaximumContentLength}.");
        }

        bool isPlainText = fileType is "txt" or "md";
        if (!isPlainText && !_writers.ContainsKey(fileType))
        {
            return ToolResult.Fail($"File type '{fileType}' is not supported. Supported types: txt, md, pdf, docx.");
        }

        string directory = Path.Combine(_options.OutputDirectory, SubFolder);
        Directory.CreateDirectory(directory);

        string name = BuildFileName(filename, fileType);
        string path = SafePath.MakeUniquePath(Path.Combine(directory, name));

        if (isPlainText)
        {
            await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), cancellationToken);
        }
        else
        {
            IDocumentWriter writer = _writers[fileType];
            List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse(content);
            await Task.Run(() => writer.Write(blocks, path), cancellationToken);
        }

        return ToolResult.Ok(
            $"Saved the {fileType.ToUpperInvariant()} file: {path}",
            new[] { new ToolArtifact(ToolArtifactKinds.File, path) });
    }

    private static string BuildFileName(string requestedName, string fileType)
    {
        string name = SafePath.SanitizeName(requestedName, "document");

        // The model sometimes includes the extension ("report.pdf"); do not produce "report.pdf.pdf".
        string suffix = "." + fileType;
        if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && name.Length > suffix.Length)
        {
            name = name.Substring(0, name.Length - suffix.Length);
        }

        return name + suffix;
    }
}

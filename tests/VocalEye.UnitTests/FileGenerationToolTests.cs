using System.Text;
using FluentAssertions;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.FileGeneration;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class FileGenerationToolTests
{
    private const string Document = "# Title\n\n## Section\n- point one\n- point **two**\n\nA closing *paragraph*.";

    [Fact]
    public async Task ExecuteAsync_TextFile_IsWrittenInsideTheDocumentsFolder()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "notes", filetype = "txt", content = "hello" }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        string expected = Path.Combine(temp.Path, "documents", "notes.txt");
        File.ReadAllText(expected).Should().Be("hello");
        result.Output.Should().Contain(expected);
    }

    [Fact]
    public async Task ExecuteAsync_NameAlreadyHasTheExtension_IsNotDoubled()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "readme.md", filetype = "md", content = "# Hi" }),
            CancellationToken.None);

        File.Exists(Path.Combine(temp.Path, "documents", "readme.md")).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SameNameTwice_DoesNotOverwrite()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);
        object arguments = new { filename = "notes", filetype = "txt", content = "first" };

        await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);
        await tool.ExecuteAsync(JsonArguments.From(new { filename = "notes", filetype = "txt", content = "second" }), CancellationToken.None);

        File.ReadAllText(Path.Combine(temp.Path, "documents", "notes.txt")).Should().Be("first");
        File.ReadAllText(Path.Combine(temp.Path, "documents", "notes (2).txt")).Should().Be("second");
    }

    [Fact]
    public async Task ExecuteAsync_Pdf_ProducesARealPdfFile()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "report", filetype = "pdf", content = Document }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        byte[] bytes = File.ReadAllBytes(Path.Combine(temp.Path, "documents", "report.pdf"));
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task ExecuteAsync_Docx_ProducesAZipPackageWithTheDocumentPart()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "report", filetype = "docx", content = Document }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        string path = Path.Combine(temp.Path, "documents", "report.docx");
        using System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(path);
        archive.Entries.Select(entry => entry.FullName).Should().Contain("word/document.xml");
    }

    [Fact]
    public async Task ExecuteAsync_FileNameWithPathParts_CannotEscapeTheFolder()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "../../outside", filetype = "txt", content = "x" }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        string documents = Path.Combine(temp.Path, "documents");
        Directory.GetFiles(documents).Should().ContainSingle();
        Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Should().OnlyContain(file => file.StartsWith(documents));
    }

    [Theory]
    [InlineData("pptx")]
    [InlineData("exe")]
    public async Task ExecuteAsync_UnsupportedType_FailsWithTheSupportedList(string fileType)
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "x", filetype = fileType, content = "x" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("txt, md, pdf, docx");
    }

    [Fact]
    public async Task ExecuteAsync_MissingContent_ThrowsArgumentException()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        Func<Task> act = () => tool.ExecuteAsync(
            JsonArguments.From(new { filename = "x", filetype = "txt" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*content*");
    }

    [Fact]
    public async Task ExecuteAsync_ContentTooLong_Fails()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "big", filetype = "txt", content = new string('a', 200_001) }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}

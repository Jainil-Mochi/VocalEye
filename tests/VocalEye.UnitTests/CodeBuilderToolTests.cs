using FluentAssertions;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.CodeBuilder;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class CodeBuilderToolTests
{
    [Fact]
    public async Task ExecuteAsync_ValidProject_WritesAllFilesIncludingNestedFolders()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new
        {
            projectName = "todo-api",
            files = new[]
            {
                new { path = "README.md", content = "# Todo API" },
                new { path = "src/app.py", content = "print('hi')" },
                new { path = "src/utils/__init__.py", content = string.Empty },
            },
        };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        result.Success.Should().BeTrue();
        string project = Path.Combine(temp.Path, "projects", "todo-api");
        File.ReadAllText(Path.Combine(project, "README.md")).Should().Be("# Todo API");
        File.ReadAllText(Path.Combine(project, "src", "app.py")).Should().Be("print('hi')");
        File.Exists(Path.Combine(project, "src", "utils", "__init__.py")).Should().BeTrue();
        result.Output.Should().Contain(project).And.Contain("src/app.py").And.Contain("Nothing was run");
    }

    [Fact]
    public async Task ExecuteAsync_SameProjectNameTwice_UsesANewNumberedFolder()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new { projectName = "demo", files = new[] { new { path = "a.txt", content = "x" } } };

        await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);
        await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        Directory.Exists(Path.Combine(temp.Path, "projects", "demo")).Should().BeTrue();
        Directory.Exists(Path.Combine(temp.Path, "projects", "demo (2)")).Should().BeTrue();
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("src/../../escape.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\x.txt")]
    public async Task ExecuteAsync_UnsafePath_ThrowsAndWritesNothing(string path)
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new
        {
            projectName = "demo",
            files = new[] { new { path = "ok.txt", content = "fine" }, new { path = path, content = "bad" } },
        };

        Func<Task> act = () => tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_DuplicatePaths_FailsWithoutWriting()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new
        {
            projectName = "demo",
            files = new[] { new { path = "a.txt", content = "1" }, new { path = "A.TXT", content = "2" } },
        };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("more than once");
        Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_PathUsedAsFileAndFolder_Fails()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new
        {
            projectName = "demo",
            files = new[] { new { path = "src", content = "1" }, new { path = "src/app.py", content = "2" } },
        };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("folder");
    }

    [Fact]
    public async Task ExecuteAsync_TooManyFiles_Fails()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object[] files = Enumerable.Range(1, 61).Select(index => (object)new { path = $"f{index}.txt", content = "x" }).ToArray();

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { projectName = "big", files = files }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Too many files");
    }

    [Fact]
    public async Task ExecuteAsync_FileTooLarge_Fails()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new
        {
            projectName = "big",
            files = new[] { new { path = "huge.txt", content = new string('a', 256 * 1024 + 1) } },
        };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("too large");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyFileList_Fails()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { projectName = "empty", files = Array.Empty<object>() }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_FilesMissing_ThrowsArgumentException()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);

        Func<Task> act = () => tool.ExecuteAsync(JsonArguments.From(new { projectName = "x" }), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*files*");
    }

    [Fact]
    public async Task ExecuteAsync_ProjectNameWithPathParts_StaysInsideTheProjectsFolder()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new { projectName = "../../outside", files = new[] { new { path = "a.txt", content = "x" } } };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        result.Success.Should().BeTrue();
        string projects = Path.Combine(temp.Path, "projects");
        Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Should().OnlyContain(file => file.StartsWith(projects));
    }
}

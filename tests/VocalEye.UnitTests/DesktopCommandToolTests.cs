using FluentAssertions;
using Moq;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.DesktopCommand;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class DesktopCommandToolTests
{
    [Fact]
    public async Task ExecuteAsync_HttpsUrl_OpensItInTheBrowser()
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = CreateTool(launcher);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_url", target = "https://www.google.com/search?q=dotnet" }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        launcher.Verify(item => item.OpenUrl(It.Is<Uri>(uri => uri.Host == "www.google.com")), Times.Once);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/cmd.exe")]
    [InlineData("ftp://example.com/file")]
    [InlineData("not a url")]
    [InlineData("calc.exe")]
    public async Task ExecuteAsync_NotAWebAddress_IsRefusedAndNothingStarts(string target)
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = CreateTool(launcher);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_url", target }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        launcher.Verify(item => item.OpenUrl(It.IsAny<Uri>()), Times.Never);
        launcher.Verify(item => item.OpenApp(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AllowedApp_StartsTheConfiguredExecutableIgnoringCase()
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = CreateTool(launcher);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_app", target = "NOTEPAD" }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        launcher.Verify(item => item.OpenApp("notepad.exe"), Times.Once);
    }

    [Theory]
    [InlineData("powershell")]
    [InlineData("cmd.exe /c del *")]
    [InlineData("C:\\Windows\\System32\\cmd.exe")]
    public async Task ExecuteAsync_AppNotOnTheList_IsRefusedAndNothingStarts(string target)
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = CreateTool(launcher);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_app", target }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not an allowed app");
        launcher.Verify(item => item.OpenApp(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_Fails()
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = CreateTool(launcher);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "run_shell", target = "dir" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown action");
    }

    [Fact]
    public async Task ExecuteAsync_ExecuteLocallyOff_RefusesEverything()
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandOptions options = CreateOptions();
        options.ExecuteLocally = false;
        DesktopCommandTool tool = new DesktopCommandTool(options, launcher.Object);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_url", target = "https://example.com" }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("turned off");
        launcher.Verify(item => item.OpenUrl(It.IsAny<Uri>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MissingTarget_ThrowsArgumentException()
    {
        DesktopCommandTool tool = CreateTool(new Mock<IDesktopLauncher>());

        Func<Task> act = () => tool.ExecuteAsync(JsonArguments.From(new { action = "open_url" }), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*target*");
    }

    [Fact]
    public void Description_ListsTheAllowedApps()
    {
        DesktopCommandTool tool = CreateTool(new Mock<IDesktopLauncher>());

        tool.Description.Should().Contain("notepad").And.Contain("calculator");
    }

    private static DesktopCommandOptions CreateOptions()
    {
        return new DesktopCommandOptions
        {
            AllowedApps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["notepad"] = "notepad.exe",
                ["calculator"] = "calc.exe",
            },
        };
    }

    private static DesktopCommandTool CreateTool(Mock<IDesktopLauncher> launcher)
    {
        return new DesktopCommandTool(CreateOptions(), launcher.Object);
    }
}

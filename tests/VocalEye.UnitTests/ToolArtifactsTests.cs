using System.Text.Json;
using Microsoft.Extensions.AI;
using FluentAssertions;
using Moq;
using VocalEye.Agents.Orchestration;
using VocalEye.Agents.Tooling;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.FileGeneration;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolArtifactsTests
{
    [Fact]
    public async Task FileGenerator_ReportsTheFileItWrote()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        FileGenerationTool tool = new FileGenerationTool(temp.Options);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { filename = "notes", filetype = "txt", content = "hello" }),
            CancellationToken.None);

        string expected = Path.Combine(temp.Path, "documents", "notes.txt");
        result.Artifacts.Should().ContainSingle().Which.Should().Be(new ToolArtifact(ToolArtifactKinds.File, expected));
    }

    [Fact]
    public async Task CodeBuilder_ReportsTheProjectFolder()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        CodeBuilderTool tool = new CodeBuilderTool(temp.Options);
        object arguments = new { projectName = "demo", files = new[] { new { path = "README.md", content = "# Demo" } } };

        ToolResult result = await tool.ExecuteAsync(JsonArguments.From(arguments), CancellationToken.None);

        string expected = Path.Combine(temp.Path, "projects", "demo");
        result.Artifacts.Should().ContainSingle().Which.Should().Be(new ToolArtifact(ToolArtifactKinds.Folder, expected));
    }

    [Fact]
    public async Task DesktopCommand_OpenUrl_ReportsTheLink()
    {
        Mock<IDesktopLauncher> launcher = new Mock<IDesktopLauncher>();
        DesktopCommandTool tool = new DesktopCommandTool(new DesktopCommandOptions(), launcher.Object);

        ToolResult result = await tool.ExecuteAsync(
            JsonArguments.From(new { action = "open_url", target = "https://example.com/a" }),
            CancellationToken.None);

        result.Artifacts.Should().ContainSingle().Which.Should().Be(new ToolArtifact(ToolArtifactKinds.Url, "https://example.com/a"));
    }

    [Fact]
    public async Task Adapter_AddsArtifactsAsALastLine_AndTheReplyBuilderTakesThemBackOut()
    {
        ToolArtifact artifact = new ToolArtifact(ToolArtifactKinds.File, @"C:\docs\report.pdf");
        Mock<IAgentTool> tool = ToolMockFactory.Create("file_generator", ToolRiskLevel.Write);
        tool.Setup(item => item.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResult.Ok("Saved the PDF file: C:\\docs\\report.pdf", new[] { artifact }));

        object? forTheModel = await new AgentToolAdapter(tool.Object).InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

        forTheModel!.ToString().Should().Contain("[artifacts]");

        FunctionCallContent call = new FunctionCallContent("call_1", "file_generator", new Dictionary<string, object?>());
        FunctionResultContent resultContent = new FunctionResultContent("call_1", forTheModel);
        List<ChatMessage> fresh = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, new List<AIContent> { call }),
            new ChatMessage(ChatRole.Tool, new List<AIContent> { resultContent }),
        };

        AgentReply reply = AgentReplyBuilder.Build("done", fresh, fresh, new ModelSelection("fake", "m1"));

        ToolInvocation invocation = reply.ToolCalls.Should().ContainSingle().Subject;
        invocation.Result.Success.Should().BeTrue();
        invocation.Result.Output.Should().Be("Saved the PDF file: C:\\docs\\report.pdf");
        invocation.Result.Artifacts.Should().Equal(artifact);
    }
}

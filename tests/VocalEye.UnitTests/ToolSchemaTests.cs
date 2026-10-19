using System.Text.Json;
using FluentAssertions;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;
using VocalEye.Tools.FileGeneration;
using VocalEye.Tools.Utility;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolSchemaTests
{
    public static IEnumerable<object[]> RealTools()
    {
        yield return new object[] { new FileGenerationTool() };
        yield return new object[] { new SendEmailTool() };
        yield return new object[] { new DesktopCommandTool() };
        yield return new object[] { new CodeBuilderTool() };
        yield return new object[] { new GetCurrentTimeTool() };
    }

    [Theory]
    [MemberData(nameof(RealTools))]
    public void ParametersJsonSchema_IsValidAndDeclaresAtLeastOneParameter(IAgentTool tool)
    {
        using JsonDocument document = JsonDocument.Parse(tool.ParametersJsonSchema);

        document.RootElement.GetProperty("type").GetString().Should().Be("object");

        // Gemini rejects tool schemas with an empty "properties" object.
        document.RootElement.GetProperty("properties").EnumerateObject().Should().NotBeEmpty($"{tool.Name} must declare a parameter");
    }

    [Fact]
    public async Task ExecuteAsync_CurrentTimeInUtc_ReturnsUtcOffset()
    {
        GetCurrentTimeTool tool = new GetCurrentTimeTool();
        JsonElement arguments = JsonDocument.Parse("""{"timezone":"UTC"}""").RootElement;

        ToolResult result = await tool.ExecuteAsync(arguments, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("+00:00");
    }

    [Fact]
    public async Task ExecuteAsync_CurrentTimeWithUnknownZone_Fails()
    {
        GetCurrentTimeTool tool = new GetCurrentTimeTool();
        JsonElement arguments = JsonDocument.Parse("""{"timezone":"Mars/Base"}""").RootElement;

        ToolResult result = await tool.ExecuteAsync(arguments, CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}

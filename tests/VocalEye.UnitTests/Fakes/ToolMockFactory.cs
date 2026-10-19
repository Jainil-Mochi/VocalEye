using System.Text.Json;
using Moq;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.UnitTests.Fakes;

internal static class ToolMockFactory
{
    public static Mock<IAgentTool> Create(string name, ToolRiskLevel risk, string output = "tool-output")
    {
        Mock<IAgentTool> tool = new Mock<IAgentTool>();
        tool.Setup(item => item.Name).Returns(name);
        tool.Setup(item => item.Description).Returns("fake tool");
        tool.Setup(item => item.Risk).Returns(risk);
        tool.Setup(item => item.ParametersJsonSchema).Returns("""{"type":"object","properties":{"note":{"type":"string"}}}""");
        tool.Setup(item => item.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolResult.Ok(output));

        return tool;
    }

    public static void VerifyExecuted(Mock<IAgentTool> tool, Times times)
    {
        tool.Verify(item => item.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()), times);
    }
}

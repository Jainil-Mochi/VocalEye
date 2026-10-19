using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using VocalEye.Agents.Guardrails;
using VocalEye.Agents.Tooling;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Registry;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class AgentToolAdapterTests
{
    [Fact]
    public void Constructor_ExposesNameDescriptionAndSchema()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("t", ToolRiskLevel.ReadOnly);

        AgentToolAdapter adapter = new AgentToolAdapter(tool.Object);

        adapter.Name.Should().Be("t");
        adapter.Description.Should().Be("fake tool");
        adapter.JsonSchema.GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public async Task InvokeAsync_RunsTheToolAndReturnsItsOutput()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("t", ToolRiskLevel.ReadOnly, "hello");

        object? result = await new AgentToolAdapter(tool.Object).InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

        result?.ToString().Should().Be("hello");
        ToolMockFactory.VerifyExecuted(tool, Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_ToolThrows_ReturnsErrorTextInsteadOfCrashingTheRun()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("boom", ToolRiskLevel.ReadOnly);
        tool.Setup(item => item.ExecuteAsync(It.IsAny<JsonElement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kaboom"));

        object? result = await new AgentToolAdapter(tool.Object).InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

        result?.ToString().Should().StartWith(AgentToolAdapter.ErrorPrefix).And.Contain("kaboom");
    }

    [Fact]
    public void RequiresApproval_OnlyForConfiguredRiskLevels()
    {
        ToolApprovalPolicy policy = new ToolApprovalPolicy(new[] { ToolRiskLevel.Destructive });

        policy.RequiresApproval(ToolMockFactory.Create("a", ToolRiskLevel.Destructive).Object).Should().BeTrue();
        policy.RequiresApproval(ToolMockFactory.Create("b", ToolRiskLevel.Write).Object).Should().BeFalse();
        policy.RequiresApproval(ToolMockFactory.Create("c", ToolRiskLevel.ReadOnly).Object).Should().BeFalse();
    }

    [Fact]
    public void Create_AdaptsEveryRegisteredTool()
    {
        ToolRegistry registry = new ToolRegistry(new[]
        {
            ToolMockFactory.Create("a", ToolRiskLevel.ReadOnly).Object,
            ToolMockFactory.Create("b", ToolRiskLevel.Destructive).Object,
        });

        List<AITool> tools = AgentToolFactory.Create(registry, new ToolApprovalPolicy(new[] { ToolRiskLevel.Destructive }));

        tools.Should().HaveCount(2);
    }
}

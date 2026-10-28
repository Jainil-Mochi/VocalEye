using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VocalEye.Agents;
using VocalEye.Agents.Guardrails;
using VocalEye.Agents.Orchestration;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Infrastructure.Llm;
using VocalEye.Infrastructure.Persistence;
using VocalEye.Tools.Registry;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class AgentOrchestratorTests
{
    [Fact]
    public async Task RunAsync_PlainTurn_ReturnsReplyAndStoresHistoryAndModel()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("hello"));
        Guid id = Guid.NewGuid();

        AgentReply reply = await rig.Orchestrator.RunAsync(new AgentRequest(id, "hi"), CancellationToken.None);

        reply.Text.Should().Be("hello");
        reply.ModelUsed.Should().Be(new ModelSelection("fake", "m1"));
        ConversationState state = await rig.Store.GetAsync(id, CancellationToken.None);
        state.Messages.Should().HaveCount(2);
        state.Model.Should().Be(new ModelSelection("fake", "m1"));
    }

    [Fact]
    public async Task RunAsync_ModelChangesBetweenTurns_UsesRequestedModelThenRemembersIt()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("ok"));
        Guid id = Guid.NewGuid();

        await rig.Orchestrator.RunAsync(new AgentRequest(id, "one"), CancellationToken.None);
        await rig.Orchestrator.RunAsync(new AgentRequest(id, "two", Model: new ModelSelection("fake", "m2")), CancellationToken.None);
        await rig.Orchestrator.RunAsync(new AgentRequest(id, "three"), CancellationToken.None);

        rig.Factory.Verify(factory => factory.Create(It.Is<ModelSelection>(model => model.Model == "m1")), Times.Once);
        rig.Factory.Verify(factory => factory.Create(It.Is<ModelSelection>(model => model.Model == "m2")), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_UnknownModel_ThrowsArgumentException()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("ok"));
        AgentRequest request = new AgentRequest(Guid.NewGuid(), "hi", Model: new ModelSelection("fake", "nope"));

        Func<Task> act = () => rig.Orchestrator.RunAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_EmptyRequest_ThrowsArgumentException()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("ok"));

        Func<Task> act = () => rig.Orchestrator.RunAsync(new AgentRequest(Guid.NewGuid(), "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_ReadOnlyTool_RunsWithoutApproval()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("get_time", ToolRiskLevel.ReadOnly, "12:00");
        Rig rig = CreateRig(
            (call, _) => call == 1 ? ScriptedChatClient.CallTool("c1", "get_time") : ScriptedChatClient.Text("It is 12:00"),
            tool.Object);

        AgentReply reply = await rig.Orchestrator.RunAsync(new AgentRequest(Guid.NewGuid(), "time?"), CancellationToken.None);

        ToolMockFactory.VerifyExecuted(tool, Times.Once());
        reply.Text.Should().Be("It is 12:00");
        reply.PendingApprovals.Should().BeEmpty();
        ToolInvocation invocation = reply.ToolCalls.Should().ContainSingle().Subject;
        invocation.Name.Should().Be("get_time");
        invocation.Result.Success.Should().BeTrue();
        invocation.Result.Output.Should().Be("12:00");
    }

    [Fact]
    public async Task RunAsync_ModelKeepsCallingTools_IsStoppedAtTheLimitAndStillAnswers()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("get_time", ToolRiskLevel.ReadOnly, "12:00");
        Rig rig = CreateRig(
            (call, messages) => messages.Any(message => message.Text.Contains("tool limit"))
                ? ScriptedChatClient.Text("I could not finish; here is what I have.")
                : ScriptedChatClient.CallTool($"c{call}", "get_time"),
            new AgentOptions { MaxToolRounds = 2 },
            tool.Object);

        AgentReply reply = await rig.Orchestrator.RunAsync(new AgentRequest(Guid.NewGuid(), "keep checking"), CancellationToken.None);

        reply.Text.Should().Be("I could not finish; here is what I have.");
        ToolMockFactory.VerifyExecuted(tool, Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_DestructiveTool_PausesForApprovalThenRunsWhenApproved()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("send_mail", ToolRiskLevel.Destructive, "sent");
        Rig rig = CreateRig(
            (call, _) => call == 1 ? ScriptedChatClient.CallTool("c1", "send_mail") : ScriptedChatClient.Text("done"),
            tool.Object);
        Guid id = Guid.NewGuid();

        AgentReply first = await rig.Orchestrator.RunAsync(new AgentRequest(id, "email bob"), CancellationToken.None);

        ToolMockFactory.VerifyExecuted(tool, Times.Never());
        PendingApproval pending = first.PendingApprovals.Should().ContainSingle().Subject;
        pending.ApprovalId.Should().Be("c1");
        pending.ToolName.Should().Be("send_mail");

        AgentReply second = await rig.Orchestrator.RunAsync(
            new AgentRequest(id, null, Approvals: new List<ApprovalDecision> { new ApprovalDecision("c1", true) }),
            CancellationToken.None);

        ToolMockFactory.VerifyExecuted(tool, Times.Once());
        second.Text.Should().Be("done");
        second.PendingApprovals.Should().BeEmpty();
        second.ToolCalls.Should().ContainSingle().Which.Name.Should().Be("send_mail");
    }

    [Fact]
    public async Task RunAsync_RejectedApproval_DoesNotRunTheTool()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("send_mail", ToolRiskLevel.Destructive);
        Rig rig = CreateRig(
            (call, _) => call == 1 ? ScriptedChatClient.CallTool("c1", "send_mail") : ScriptedChatClient.Text("Okay, I will not send it."),
            tool.Object);
        Guid id = Guid.NewGuid();

        await rig.Orchestrator.RunAsync(new AgentRequest(id, "email bob"), CancellationToken.None);
        AgentReply reply = await rig.Orchestrator.RunAsync(
            new AgentRequest(id, null, Approvals: new List<ApprovalDecision> { new ApprovalDecision("c1", false) }),
            CancellationToken.None);

        ToolMockFactory.VerifyExecuted(tool, Times.Never());
        reply.Text.Should().Be("Okay, I will not send it.");
    }

    [Fact]
    public async Task RunAsync_UnknownApprovalId_ThrowsArgumentException()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("ok"));
        AgentRequest request = new AgentRequest(
            Guid.NewGuid(),
            null,
            Approvals: new List<ApprovalDecision> { new ApprovalDecision("nope", true) });

        Func<Task> act = () => rig.Orchestrator.RunAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_ModelFailure_BubblesUpWithoutLocalWrapping()
    {
        Rig rig = CreateRig((_, _) => throw new HttpRequestException("connection refused"));

        Func<Task> act = () => rig.Orchestrator.RunAsync(new AgentRequest(Guid.NewGuid(), "hi"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*connection refused*");
    }

    [Fact]
    public async Task RunStreamingAsync_PlainTurn_YieldsTextThenCompletedAndStoresHistory()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("hello"));
        Guid id = Guid.NewGuid();

        List<AgentStreamEvent> events = await CollectAsync(rig.Orchestrator.RunStreamingAsync(new AgentRequest(id, "hi"), CancellationToken.None));

        events.Should().NotBeEmpty();
        events.Last().Kind.Should().Be(AgentStreamEventKind.Completed);
        events.Last().Reply!.Text.Should().Be("hello");
        string streamedText = string.Concat(events.Where(item => item.Kind == AgentStreamEventKind.TextDelta).Select(item => item.Text));
        streamedText.Should().Be("hello");
        ConversationState state = await rig.Store.GetAsync(id, CancellationToken.None);
        state.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunStreamingAsync_DestructiveTool_CompletesWithPendingApproval()
    {
        Mock<IAgentTool> tool = ToolMockFactory.Create("send_mail", ToolRiskLevel.Destructive, "sent");
        Rig rig = CreateRig(
            (call, _) => call == 1 ? ScriptedChatClient.CallTool("c1", "send_mail") : ScriptedChatClient.Text("done"),
            tool.Object);

        List<AgentStreamEvent> events = await CollectAsync(rig.Orchestrator.RunStreamingAsync(new AgentRequest(Guid.NewGuid(), "email bob"), CancellationToken.None));

        ToolMockFactory.VerifyExecuted(tool, Times.Never());
        AgentReply reply = events.Last().Reply!;
        reply.PendingApprovals.Should().ContainSingle().Which.ApprovalId.Should().Be("c1");
    }

    [Fact]
    public async Task RunStreamingAsync_EmptyRequest_ThrowsOnFirstMoveNext()
    {
        Rig rig = CreateRig((_, _) => ScriptedChatClient.Text("ok"));

        Func<Task> act = () => CollectAsync(rig.Orchestrator.RunStreamingAsync(new AgentRequest(Guid.NewGuid(), " "), CancellationToken.None));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static async Task<List<AgentStreamEvent>> CollectAsync(IAsyncEnumerable<AgentStreamEvent> source)
    {
        List<AgentStreamEvent> events = new List<AgentStreamEvent>();
        await foreach (AgentStreamEvent item in source)
        {
            events.Add(item);
        }

        return events;
    }

    private static Rig CreateRig(
        Func<int, IReadOnlyList<ChatMessage>, ChatResponse> script,
        params IAgentTool[] tools)
    {
        return CreateRig(script, new AgentOptions(), tools);
    }

    private static Rig CreateRig(
        Func<int, IReadOnlyList<ChatMessage>, ChatResponse> script,
        AgentOptions agentOptions,
        params IAgentTool[] tools)
    {
        ModelCatalog catalog = new ModelCatalog(Options.Create(new LlmOptions
        {
            Default = new DefaultModelOptions { Provider = "fake", Model = "m1" },
            Providers =
            {
                ["fake"] = new ProviderOptions
                {
                    Type = "OpenAI",
                    ApiKey = "key",
                    Models = new List<ModelOptions> { new ModelOptions { Id = "m1" }, new ModelOptions { Id = "m2" } },
                },
            },
        }));

        ScriptedChatClient client = new ScriptedChatClient(script);
        Mock<IChatClientFactory> factory = new Mock<IChatClientFactory>();
        factory.Setup(item => item.Create(It.IsAny<ModelSelection>())).Returns(client);

        ConversationStore store = new ConversationStore(new InMemoryConversationRepository());
        ToolApprovalPolicy policy = new ToolApprovalPolicy(new[] { ToolRiskLevel.Write, ToolRiskLevel.Destructive });

        AgentOrchestrator orchestrator = new AgentOrchestrator(
            factory.Object,
            catalog,
            new ToolRegistry(tools),
            policy,
            store,
            agentOptions,
            NullLogger<AgentOrchestrator>.Instance);

        return new Rig(orchestrator, factory, store);
    }

    private sealed class Rig
    {
        public Rig(AgentOrchestrator orchestrator, Mock<IChatClientFactory> factory, ConversationStore store)
        {
            Orchestrator = orchestrator;
            Factory = factory;
            Store = store;
        }

        public AgentOrchestrator Orchestrator { get; }

        public Mock<IChatClientFactory> Factory { get; }

        public ConversationStore Store { get; }
    }
}

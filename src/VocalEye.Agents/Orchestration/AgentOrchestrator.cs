using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VocalEye.Agents.Prompts;
using VocalEye.Agents.Tooling;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Orchestration;

/// <summary>
/// Runs one conversation turn on Microsoft Agent Framework.
/// The agent is rebuilt every turn for the chosen model and is given the full stored history, so the model can
/// change between turns and the service stays stateless (ADR-0007).
/// </summary>
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IChatClientFactory _clients;
    private readonly IModelCatalog _catalog;
    private readonly IToolRegistry _tools;
    private readonly IToolApprovalPolicy _approvalPolicy;
    private readonly IConversationStore _store;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IChatClientFactory clients,
        IModelCatalog catalog,
        IToolRegistry tools,
        IToolApprovalPolicy approvalPolicy,
        IConversationStore store,
        ILogger<AgentOrchestrator> logger)
    {
        _clients = clients;
        _catalog = catalog;
        _tools = tools;
        _approvalPolicy = approvalPolicy;
        _store = store;
        _logger = logger;
    }

    public async Task<AgentReply> RunAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        TurnContext turn = await PrepareTurnAsync(request, cancellationToken);

        AgentResponse response = await turn.Agent.RunAsync(turn.History, cancellationToken: cancellationToken);

        return await CompleteTurnAsync(turn, response, cancellationToken);
    }

    public async IAsyncEnumerable<AgentStreamEvent> RunStreamingAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        TurnContext turn = await PrepareTurnAsync(request, cancellationToken);

        List<AgentResponseUpdate> updates = new List<AgentResponseUpdate>();
        await foreach (AgentResponseUpdate update in turn.Agent.RunStreamingAsync(turn.History, cancellationToken: cancellationToken))
        {
            updates.Add(update);

            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return AgentStreamEvent.ForText(update.Text);
            }
        }

        // Rebuild the complete messages (text, tool calls, results, approval requests) so they can be stored.
        AgentResponse response = updates.ToAgentResponse();
        AgentReply reply = await CompleteTurnAsync(turn, response, cancellationToken);

        yield return AgentStreamEvent.ForCompleted(reply);
    }

    private async Task<TurnContext> PrepareTurnAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        bool hasText = !string.IsNullOrWhiteSpace(request.UserInput);
        bool hasApprovals = request.Approvals is { Count: > 0 };

        if (!hasText && !hasApprovals)
        {
            throw new ArgumentException("Provide a message or approval decisions.", nameof(request));
        }

        if (_catalog.List().Count == 0)
        {
            throw new ArgumentException(
                "No AI models are configured. Set at least one provider API key and restart, e.g. " +
                "dotnet user-secrets set \"Llm:Providers:groq:ApiKey\" \"<key>\" --project src/VocalEye.Api",
                nameof(request));
        }

        ConversationState state = await _store.GetAsync(request.ConversationId, cancellationToken);

        ModelSelection selection = request.Model ?? state.Model ?? _catalog.Default;
        ModelInfo info = _catalog.Find(selection)
            ?? throw new ArgumentException($"Model '{selection.Provider}/{selection.Model}' is not available.", nameof(request));

        List<ChatMessage> input = new List<ChatMessage>();
        if (hasApprovals)
        {
            input.Add(ApprovalMessageBuilder.Build(state.Messages, request.Approvals!));
        }

        if (hasText)
        {
            input.Add(new ChatMessage(ChatRole.User, request.UserInput!));
        }

        // A model without tool support still works; it just cannot act.
        List<AITool>? agentTools = info.SupportsTools ? AgentToolFactory.Create(_tools, _approvalPolicy) : null;

        AIAgent agent = new ChatClientAgent(
            _clients.Create(selection),
            instructions: SystemPrompts.Assistant,
            name: "VocalEye",
            tools: agentTools);

        List<ChatMessage> history = state.Messages.Concat(input).ToList();

        _logger.LogInformation(
            "Agent turn {ConversationId} model={Provider}/{Model} mode={Mode} tools={ToolCount} historyMessages={History}",
            request.ConversationId,
            selection.Provider,
            selection.Model,
            request.Mode,
            agentTools?.Count ?? 0,
            history.Count);

        return new TurnContext(state, selection, agent, input, history, request, agentTools?.Count ?? 0);
    }

    private async Task<AgentReply> CompleteTurnAsync(TurnContext turn, AgentResponse response, CancellationToken cancellationToken)
    {
        turn.State.Messages.AddRange(turn.Input);
        turn.State.Messages.AddRange(response.Messages);
        turn.State.Model = turn.Selection;
        await _store.SaveAsync(turn.State, cancellationToken);

        AgentReply reply = AgentReplyBuilder.Build(response.Text, response.Messages.ToList(), turn.History, turn.Selection);

        _logger.LogInformation(
            "Agent turn {ConversationId} done: toolCalls={ToolCalls} pendingApprovals={Pending}",
            turn.Request.ConversationId,
            reply.ToolCalls.Count,
            reply.PendingApprovals.Count);

        return reply;
    }
}

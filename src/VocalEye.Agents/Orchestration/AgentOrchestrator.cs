using System.Text.Json;
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
public sealed class AgentOrchestrator(
    IChatClientFactory clients,
    IModelCatalog catalog,
    IToolRegistry tools,
    IToolApprovalPolicy approvalPolicy,
    IConversationStore store,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    public async Task<AgentReply> RunAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        var hasText = !string.IsNullOrWhiteSpace(request.UserInput);
        var hasApprovals = request.Approvals is { Count: > 0 };
        if (!hasText && !hasApprovals)
            throw new ArgumentException("Provide a message or approval decisions.", nameof(request));

        var state = await store.GetAsync(request.ConversationId, cancellationToken);

        var selection = request.Model ?? state.Model ?? catalog.Default;
        var info = catalog.Find(selection)
            ?? throw new ArgumentException($"Model '{selection.Provider}/{selection.Model}' is not available.", nameof(request));

        var input = new List<ChatMessage>();
        if (hasApprovals)
            input.Add(BuildApprovalMessage(state, request.Approvals!));
        if (hasText)
            input.Add(new ChatMessage(ChatRole.User, request.UserInput!));

        // A model without tool support still works; it just can't act.
        var agentTools = info.SupportsTools ? AgentToolFactory.Create(tools, approvalPolicy) : null;

        AIAgent agent = new ChatClientAgent(
            clients.Create(selection),
            instructions: SystemPrompts.Assistant,
            name: "VocalEye",
            tools: agentTools);

        var history = state.Messages.Concat(input).ToList();

        logger.LogInformation(
            "Agent turn {ConversationId} model={Provider}/{Model} mode={Mode} tools={ToolCount} historyMessages={History}",
            request.ConversationId, selection.Provider, selection.Model, request.Mode, agentTools?.Count ?? 0, history.Count);

        AgentResponse response;
        try
        {
            response = await agent.RunAsync(history, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Model call failed for {Provider}/{Model}", selection.Provider, selection.Model);
            throw new ModelCallFailedException(selection, ex);
        }

        state.Messages.AddRange(input);
        state.Messages.AddRange(response.Messages);
        state.Model = selection;
        await store.SaveAsync(state, cancellationToken);

        var reply = BuildReply(response, history, selection);
        logger.LogInformation(
            "Agent turn {ConversationId} done: toolCalls={ToolCalls} pendingApprovals={Pending}",
            request.ConversationId, reply.ToolCalls.Count, reply.PendingApprovals.Count);
        return reply;
    }

    /// <summary>Turns the user's Approve/Deny answers into the message the framework expects to resume the paused run.</summary>
    private static ChatMessage BuildApprovalMessage(ConversationState state, IReadOnlyList<ApprovalDecision> decisions)
    {
        var requests = state.Messages
            .SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>()
            .Where(r => r.ToolCall is FunctionCallContent)
            .GroupBy(r => ((FunctionCallContent)r.ToolCall).CallId)
            .ToDictionary(g => g.Key, g => g.Last());

        var contents = new List<AIContent>();
        foreach (var decision in decisions)
        {
            if (!requests.TryGetValue(decision.ApprovalId, out var pending))
                throw new InvalidOperationException($"No pending approval with id '{decision.ApprovalId}'.");
            contents.Add(pending.CreateResponse(decision.Approved));
        }
        return new ChatMessage(ChatRole.User, contents);
    }

    private static AgentReply BuildReply(AgentResponse response, IReadOnlyList<ChatMessage> history, ModelSelection model)
    {
        var fresh = response.Messages.SelectMany(m => m.Contents).ToList();

        // A tool result may belong to a call made in an earlier turn (approval resume), so look calls up in everything.
        var calls = history.SelectMany(m => m.Contents).Concat(fresh)
            .OfType<FunctionCallContent>()
            .GroupBy(c => c.CallId)
            .ToDictionary(g => g.Key, g => g.First());

        var invocations = fresh.OfType<FunctionResultContent>().Select(result =>
        {
            calls.TryGetValue(result.CallId, out var call);
            var text = result.Result?.ToString() ?? string.Empty;
            var failed = text.StartsWith(AgentToolAdapter.ErrorPrefix, StringComparison.Ordinal);
            return new ToolInvocation(
                call?.Name ?? "unknown",
                ArgumentsJson(call),
                failed ? ToolResult.Fail(text[AgentToolAdapter.ErrorPrefix.Length..]) : ToolResult.Ok(text));
        }).ToList();

        var pending = fresh.OfType<ToolApprovalRequestContent>()
            .Select(r => r.ToolCall as FunctionCallContent)
            .OfType<FunctionCallContent>()
            .Select(c => new PendingApproval(c.CallId, c.Name, ArgumentsJson(c)))
            .ToList();

        return new AgentReply(response.Text, model, invocations, pending);
    }

    private static string ArgumentsJson(FunctionCallContent? call)
        => JsonSerializer.Serialize(call?.Arguments ?? new Dictionary<string, object?>());
}

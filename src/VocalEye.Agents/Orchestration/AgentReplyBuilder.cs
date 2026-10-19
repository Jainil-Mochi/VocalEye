using System.Text.Json;
using Microsoft.Extensions.AI;
using VocalEye.Agents.Tooling;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Orchestration;

public static class AgentReplyBuilder
{
    /// <param name="text">The assistant's text for this turn.</param>
    /// <param name="fresh">Messages produced during this turn.</param>
    /// <param name="history">Everything sent to the model (a tool result may belong to a call made in an earlier turn).</param>
    public static AgentReply Build(string text, IReadOnlyList<ChatMessage> fresh, IReadOnlyList<ChatMessage> history, ModelSelection model)
    {
        List<AIContent> freshContents = fresh.SelectMany(message => message.Contents).ToList();

        Dictionary<string, FunctionCallContent> calls = history
            .SelectMany(message => message.Contents)
            .Concat(freshContents)
            .OfType<FunctionCallContent>()
            .GroupBy(call => call.CallId)
            .ToDictionary(group => group.Key, group => group.First());

        List<ToolInvocation> invocations = freshContents
            .OfType<FunctionResultContent>()
            .Select(result => ToInvocation(result, calls))
            .ToList();

        List<PendingApproval> pending = freshContents
            .OfType<ToolApprovalRequestContent>()
            .Select(request => request.ToolCall as FunctionCallContent)
            .OfType<FunctionCallContent>()
            .Select(ToPendingApproval)
            .ToList();

        return new AgentReply(text, model, invocations, pending);
    }

    private static ToolInvocation ToInvocation(FunctionResultContent result, Dictionary<string, FunctionCallContent> calls)
    {
        calls.TryGetValue(result.CallId, out FunctionCallContent? call);
        string output = result.Result?.ToString() ?? string.Empty;
        bool failed = output.StartsWith(AgentToolAdapter.ErrorPrefix, StringComparison.Ordinal);

        ToolResult toolResult = failed
            ? ToolResult.Fail(output[AgentToolAdapter.ErrorPrefix.Length..])
            : ToolResult.Ok(output);

        return new ToolInvocation(call?.Name ?? "unknown", ArgumentsJson(call), toolResult);
    }

    private static PendingApproval ToPendingApproval(FunctionCallContent call)
    {
        return new PendingApproval(call.CallId, call.Name, ArgumentsJson(call));
    }

    private static string ArgumentsJson(FunctionCallContent? call)
    {
        return JsonSerializer.Serialize(call?.Arguments ?? new Dictionary<string, object?>());
    }
}

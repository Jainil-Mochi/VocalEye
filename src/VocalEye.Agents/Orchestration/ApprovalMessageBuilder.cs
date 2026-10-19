using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Agents;

namespace VocalEye.Agents.Orchestration;

public static class ApprovalMessageBuilder
{
    /// <summary>Turns the user's Approve/Deny answers into the message the framework expects to resume a paused run.</summary>
    /// <exception cref="ArgumentException">A decision does not match any pending approval request.</exception>
    public static ChatMessage Build(IEnumerable<ChatMessage> history, IReadOnlyList<ApprovalDecision> decisions)
    {
        Dictionary<string, ToolApprovalRequestContent> requests = history
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>()
            .Where(request => request.ToolCall is FunctionCallContent)
            .GroupBy(request => ((FunctionCallContent)request.ToolCall).CallId)
            .ToDictionary(group => group.Key, group => group.Last());

        List<AIContent> contents = new List<AIContent>();
        foreach (ApprovalDecision decision in decisions)
        {
            if (!requests.TryGetValue(decision.ApprovalId, out ToolApprovalRequestContent? pending))
            {
                throw new ArgumentException($"No pending approval with id '{decision.ApprovalId}'.");
            }

            contents.Add(pending.CreateResponse(decision.Approved));
        }

        return new ChatMessage(ChatRole.User, contents);
    }
}

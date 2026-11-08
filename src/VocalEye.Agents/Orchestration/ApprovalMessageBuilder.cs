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

    /// <summary>
    /// Builds the approval answers for a turn. Every request still waiting must be answered for the framework to continue, so a
    /// request the client did not answer (for example the user typed a new message instead) is denied.
    /// Returns null when nothing is waiting and no decisions were sent.
    /// </summary>
    /// <exception cref="ArgumentException">A decision does not match a request that is waiting for an answer.</exception>
    public static ChatMessage? BuildComplete(IReadOnlyList<ChatMessage> stored, IReadOnlyList<ApprovalDecision>? decisions)
    {
        IReadOnlyList<ToolApprovalRequestContent> live = ApprovalHistory.FindLiveRequests(stored);
        List<ApprovalDecision> answers = decisions?.ToList() ?? new List<ApprovalDecision>();

        HashSet<string> liveIds = live
            .Where(request => request.ToolCall is FunctionCallContent)
            .Select(request => ((FunctionCallContent)request.ToolCall).CallId)
            .ToHashSet();

        foreach (ApprovalDecision decision in answers)
        {
            if (!liveIds.Contains(decision.ApprovalId))
            {
                throw new ArgumentException($"No pending approval with id '{decision.ApprovalId}'.");
            }
        }

        if (liveIds.Count == 0)
        {
            return null;
        }

        HashSet<string> answeredIds = answers.Select(decision => decision.ApprovalId).ToHashSet();
        foreach (string unansweredId in liveIds.Where(id => !answeredIds.Contains(id)))
        {
            answers.Add(new ApprovalDecision(unansweredId, false));
        }

        return Build(stored, answers);
    }

}

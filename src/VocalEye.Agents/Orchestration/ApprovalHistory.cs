using Microsoft.Extensions.AI;

namespace VocalEye.Agents.Orchestration;

/// <summary>
/// Prepares stored history for replay. Only an approval request that is still waiting for an answer may be sent to the
/// framework; answered ones are already represented by the function call and result that follow them.
/// </summary>
public static class ApprovalHistory
{
    /// <summary>Approval requests waiting for an answer: those in the last stored message, when it is an assistant message.</summary>
    public static IReadOnlyList<ToolApprovalRequestContent> FindLiveRequests(IReadOnlyList<ChatMessage> stored)
    {
        if (stored.Count == 0)
        {
            return Array.Empty<ToolApprovalRequestContent>();
        }

        ChatMessage last = stored[stored.Count - 1];
        if (last.Role != ChatRole.Assistant)
        {
            return Array.Empty<ToolApprovalRequestContent>();
        }

        return last.Contents.OfType<ToolApprovalRequestContent>().ToList();
    }

    /// <summary>
    /// Returns a copy of the history without answered (or orphaned) approval requests and without approval responses,
    /// so the framework never sees an approval request it cannot match. Stored messages are not modified.
    /// </summary>
    public static List<ChatMessage> RemoveResolved(IReadOnlyList<ChatMessage> stored)
    {
        HashSet<ToolApprovalRequestContent> live = new HashSet<ToolApprovalRequestContent>(
            FindLiveRequests(stored),
            ReferenceEqualityComparer.Instance);

        List<ChatMessage> result = new List<ChatMessage>(stored.Count);
        foreach (ChatMessage message in stored)
        {
            bool hasApprovalContent = message.Contents.Any(IsApprovalContent);
            if (!hasApprovalContent)
            {
                result.Add(message);
                continue;
            }

            List<AIContent> kept = message.Contents
                .Where(content => !IsApprovalContent(content) || (content is ToolApprovalRequestContent request && live.Contains(request)))
                .ToList();

            if (kept.Count == 0)
            {
                continue;
            }

            result.Add(new ChatMessage(message.Role, kept)
            {
                AuthorName = message.AuthorName,
                CreatedAt = message.CreatedAt,
                MessageId = message.MessageId,
                AdditionalProperties = message.AdditionalProperties,
            });
        }

        return result;
    }

    private static bool IsApprovalContent(AIContent content)
    {
        return content is ToolApprovalRequestContent || content is ToolApprovalResponseContent;
    }
}

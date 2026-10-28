using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Domain.Conversations;

namespace VocalEye.Application.Conversations;

public static class PendingApprovalFinder
{
    /// <summary>
    /// The tool calls still waiting for the user. A paused turn always ends with the assistant message that holds the
    /// approval requests; once the user answers, more messages follow, so only the last stored message can be pending.
    /// </summary>
    public static IReadOnlyList<PendingApproval> Find(IReadOnlyList<StoredMessageDto> messages)
    {
        StoredMessageDto? last = messages.OrderBy(message => message.Sequence).LastOrDefault();
        if (last is null || last.Role != MessageRole.Assistant)
        {
            return Array.Empty<PendingApproval>();
        }

        ChatMessage message = ChatMessageMapper.FromJson(last.ContentJson);

        return message.Contents
            .OfType<ToolApprovalRequestContent>()
            .Select(request => request.ToolCall as FunctionCallContent)
            .OfType<FunctionCallContent>()
            .Select(call => new PendingApproval(call.CallId, call.Name, ToolCallArguments.ToJson(call)))
            .ToList();
    }
}

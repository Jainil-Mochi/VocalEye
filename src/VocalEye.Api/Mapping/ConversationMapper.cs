using Microsoft.Extensions.AI;
using VocalEye.Application.Conversations;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Mapping;

public static class ConversationMapper
{
    public static ConversationListItemDto ToListItem(ConversationSummaryDto summary)
    {
        bool hasModel = summary.ModelProvider is not null && summary.ModelName is not null;

        return new ConversationListItemDto
        {
            ConversationId = summary.Id,
            Model = hasModel
                ? new ModelSelectionDto { Provider = summary.ModelProvider!, Model = summary.ModelName! }
                : null,
            UpdatedAt = summary.UpdatedAt,
            MessageCount = summary.MessageCount,
        };
    }

    public static ConversationMessageDto ToMessage(StoredMessageDto stored)
    {
        ChatMessage message = ChatMessageMapper.FromJson(stored.ContentJson);

        return new ConversationMessageDto
        {
            Sequence = stored.Sequence,
            Role = stored.Role.ToString().ToLowerInvariant(),
            Text = message.Text,
            ToolNames = message.Contents.OfType<FunctionCallContent>().Select(call => call.Name).ToList(),
        };
    }
}

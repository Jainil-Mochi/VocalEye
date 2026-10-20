using VocalEye.Application.Conversations;

namespace VocalEye.Application.Abstractions.Memory;

/// <summary>Read-only views of stored conversations. Implementations must not track entities and must project into DTOs.</summary>
public interface IConversationQueries
{
    /// <summary>Most recently updated conversations first.</summary>
    Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(int take, CancellationToken cancellationToken);

    /// <summary>The messages in order, or null when the conversation does not exist.</summary>
    Task<IReadOnlyList<StoredMessageDto>?> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken);
}

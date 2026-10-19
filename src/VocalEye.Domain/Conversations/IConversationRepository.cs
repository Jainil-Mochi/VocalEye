namespace VocalEye.Domain.Conversations;

public interface IConversationRepository
{
    /// <summary>Loads a conversation with its messages for updating (change-tracked). Null when it does not exist.</summary>
    Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

namespace VocalEye.Domain.Conversations;

public interface IConversationRepository
{
    /// <summary>Loads a conversation with its messages for updating (change-tracked). Null when it does not exist.</summary>
    Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);

    /// <summary>Sets the title without loading the messages. False when the conversation does not exist.</summary>
    Task<bool> RenameAsync(Guid conversationId, string title, CancellationToken cancellationToken);

    /// <summary>Deletes the conversation and all its messages. False when it does not exist.</summary>
    Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

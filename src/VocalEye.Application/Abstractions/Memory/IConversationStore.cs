namespace VocalEye.Application.Abstractions.Memory;

public interface IConversationStore
{
    /// <summary>Returns the stored conversation, or a new empty one if the id is unknown.</summary>
    Task<ConversationState> GetAsync(Guid conversationId, CancellationToken cancellationToken);

    Task SaveAsync(ConversationState state, CancellationToken cancellationToken);
}

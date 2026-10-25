namespace VocalEye.Application.Abstractions.Memory;

/// <summary>Changes to a conversation the user makes from a client: rename and delete.</summary>
public interface IConversationManager
{
    /// <returns>False when the conversation does not exist.</returns>
    /// <exception cref="ArgumentException">The title is blank or too long.</exception>
    Task<bool> RenameAsync(Guid conversationId, string title, CancellationToken cancellationToken);

    /// <returns>False when the conversation does not exist.</returns>
    Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken);
}

using VocalEye.Application.Abstractions.Memory;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

public sealed class ConversationManager : IConversationManager
{
    private readonly IConversationRepository _repository;

    public ConversationManager(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> RenameAsync(Guid conversationId, string title, CancellationToken cancellationToken)
    {
        string normalized = Conversation.NormalizeTitle(title);

        return await _repository.RenameAsync(conversationId, normalized, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(conversationId, cancellationToken).ConfigureAwait(false);
    }
}

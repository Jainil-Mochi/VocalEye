using System.Collections.Concurrent;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>Used when Persistence:Provider is InMemory (no database needed). Data is lost on restart.</summary>
public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly ConcurrentDictionary<Guid, Conversation> _items = new ConcurrentDictionary<Guid, Conversation>();

    /// <summary>Everything stored so far (read side of the in-memory provider).</summary>
    public IReadOnlyCollection<Conversation> All => _items.Values.ToList();

    public Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        _items.TryGetValue(conversationId, out Conversation? conversation);
        return Task.FromResult(conversation);
    }

    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        _items[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

using System.Collections.Concurrent;
using VocalEye.Application.Abstractions.Memory;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>Development store: lost on restart. Replaced by Postgres in a later slice (same interface).</summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, ConversationState> _items = new ConcurrentDictionary<Guid, ConversationState>();

    public Task<ConversationState> GetAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        ConversationState copy = new ConversationState { Id = conversationId };

        if (_items.TryGetValue(conversationId, out ConversationState? stored))
        {
            copy.Model = stored.Model;
            copy.Messages.AddRange(stored.Messages);
        }

        return Task.FromResult(copy);
    }

    public Task SaveAsync(ConversationState state, CancellationToken cancellationToken)
    {
        ConversationState copy = new ConversationState { Id = state.Id, Model = state.Model };
        copy.Messages.AddRange(state.Messages);
        _items[state.Id] = copy;

        return Task.CompletedTask;
    }
}

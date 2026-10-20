using Microsoft.EntityFrameworkCore;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly VocalEyeDbContext _context;

    public ConversationRepository(VocalEyeDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        // Tracked on purpose: the caller appends messages and saves. Read-only queries must use AsNoTracking().
        return await _context.Conversations
            .Include(conversation => conversation.Messages)
            .FirstOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

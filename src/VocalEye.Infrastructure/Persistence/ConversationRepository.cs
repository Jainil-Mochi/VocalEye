using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<bool> RenameAsync(Guid conversationId, string title, CancellationToken cancellationToken)
    {
        int changed = await _context.Conversations
            .Where(conversation => conversation.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(conversation => conversation.Title, title), cancellationToken)
            .ConfigureAwait(false);

        return changed > 0;
    }

    public async Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await _context.Messages
            .Where(message => message.ConversationId == conversationId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        int deleted = await _context.Conversations
            .Where(conversation => conversation.Id == conversationId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return deleted > 0;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

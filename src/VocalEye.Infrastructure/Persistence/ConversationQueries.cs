using Microsoft.EntityFrameworkCore;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>Postgres read side: no change tracking, and results are projected straight into DTOs.</summary>
public sealed class ConversationQueries : IConversationQueries
{
    private readonly VocalEyeDbContext _context;

    public ConversationQueries(VocalEyeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(int take, CancellationToken cancellationToken)
    {
        return await _context.Conversations
            .AsNoTracking()
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Take(take)
            .Select(conversation => new ConversationSummaryDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                ModelProvider = conversation.ModelProvider,
                ModelName = conversation.ModelName,
                UpdatedAt = conversation.UpdatedAt,
                MessageCount = conversation.Messages.Count(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoredMessageDto>?> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        bool exists = await _context.Conversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return null;
        }

        return await _context.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.Sequence)
            .Select(message => new StoredMessageDto
            {
                Sequence = message.Sequence,
                Role = message.Role,
                ContentJson = message.ContentJson,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

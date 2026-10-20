using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

public sealed class InMemoryConversationQueries : IConversationQueries
{
    private readonly InMemoryConversationRepository _repository;

    public InMemoryConversationQueries(InMemoryConversationRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ConversationSummaryDto>> ListAsync(int take, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConversationSummaryDto> items = _repository.All
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Take(take)
            .Select(conversation => new ConversationSummaryDto
            {
                Id = conversation.Id,
                ModelProvider = conversation.ModelProvider,
                ModelName = conversation.ModelName,
                UpdatedAt = conversation.UpdatedAt,
                MessageCount = conversation.Messages.Count,
            })
            .ToList();

        return Task.FromResult(items);
    }

    public async Task<IReadOnlyList<StoredMessageDto>?> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        Conversation? conversation = await _repository.GetByIdAsync(conversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            return null;
        }

        return conversation.Messages
            .OrderBy(message => message.Sequence)
            .Select(message => new StoredMessageDto
            {
                Sequence = message.Sequence,
                Role = message.Role,
                ContentJson = message.ContentJson,
            })
            .ToList();
    }
}

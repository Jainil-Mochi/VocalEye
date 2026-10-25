using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>
/// Stores the agent's provider-neutral history through the conversation repository (Postgres or in memory).
/// History is append-only: a save writes only the messages that are not stored yet.
/// </summary>
public sealed class ConversationStore : IConversationStore
{
    private readonly IConversationRepository _repository;

    public ConversationStore(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConversationState> GetAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        ConversationState state = new ConversationState { Id = conversationId };

        Conversation? conversation = await _repository.GetByIdAsync(conversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            return state;
        }

        foreach (Message message in conversation.Messages.OrderBy(item => item.Sequence))
        {
            state.Messages.Add(ChatMessageMapper.FromJson(message.ContentJson));
        }

        if (conversation.ModelProvider is not null && conversation.ModelName is not null)
        {
            state.Model = new ModelSelection(conversation.ModelProvider, conversation.ModelName);
        }

        return state;
    }

    public async Task SaveAsync(ConversationState state, CancellationToken cancellationToken)
    {
        Conversation? conversation = await _repository.GetByIdAsync(state.Id, cancellationToken).ConfigureAwait(false);
        bool isNew = conversation is null;
        conversation ??= Conversation.Create(state.Id);

        foreach (ChatMessage message in state.Messages.Skip(conversation.Messages.Count))
        {
            conversation.AddMessage(ChatMessageMapper.ToRole(message.Role), ChatMessageMapper.ToJson(message));
        }

        // Also fills the title of conversations stored before titles existed.
        conversation.SetTitleIfMissing(state.Messages
            .FirstOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))?.Text);

        if (state.Model is not null)
        {
            conversation.SetModel(state.Model.Provider, state.Model.Model);
        }

        if (isNew)
        {
            await _repository.AddAsync(conversation, cancellationToken).ConfigureAwait(false);
        }

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

using FluentAssertions;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Infrastructure.Persistence;
using Xunit;

namespace VocalEye.UnitTests;

public class ConversationStoreTests
{
    [Fact]
    public async Task GetAsync_UnknownConversation_ReturnsEmptyState()
    {
        ConversationStore store = new ConversationStore(new InMemoryConversationRepository());
        Guid id = Guid.NewGuid();

        ConversationState state = await store.GetAsync(id, CancellationToken.None);

        state.Id.Should().Be(id);
        state.Messages.Should().BeEmpty();
        state.Model.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsMessagesInOrderAndModel()
    {
        ConversationStore store = new ConversationStore(new InMemoryConversationRepository());
        ConversationState state = new ConversationState { Id = Guid.NewGuid(), Model = new ModelSelection("fake", "m1") };
        state.Messages.Add(new ChatMessage(ChatRole.User, "first"));
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, "second"));

        await store.SaveAsync(state, CancellationToken.None);
        ConversationState loaded = await store.GetAsync(state.Id, CancellationToken.None);

        loaded.Messages.Select(message => message.Text).Should().Equal("first", "second");
        loaded.Model.Should().Be(new ModelSelection("fake", "m1"));
    }

    [Fact]
    public async Task SaveAsync_CalledTwice_AppendsOnlyNewMessages()
    {
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationStore store = new ConversationStore(repository);
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, "one"));
        await store.SaveAsync(state, CancellationToken.None);

        ConversationState reloaded = await store.GetAsync(state.Id, CancellationToken.None);
        reloaded.Messages.Add(new ChatMessage(ChatRole.Assistant, "two"));
        await store.SaveAsync(reloaded, CancellationToken.None);

        InMemoryConversationQueries queries = new InMemoryConversationQueries(repository);
        IReadOnlyList<StoredMessageDto>? stored = await queries.GetMessagesAsync(state.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Select(message => message.Sequence).Should().Equal(0, 1);
    }

    [Fact]
    public async Task ListAsync_ReturnsNewestConversationFirstWithMessageCount()
    {
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationStore store = new ConversationStore(repository);
        ConversationState older = new ConversationState { Id = Guid.NewGuid() };
        older.Messages.Add(new ChatMessage(ChatRole.User, "old"));
        await store.SaveAsync(older, CancellationToken.None);
        await Task.Delay(5);
        ConversationState newer = new ConversationState { Id = Guid.NewGuid() };
        newer.Messages.Add(new ChatMessage(ChatRole.User, "new"));
        newer.Messages.Add(new ChatMessage(ChatRole.Assistant, "reply"));
        await store.SaveAsync(newer, CancellationToken.None);

        IReadOnlyList<ConversationSummaryDto> list = await new InMemoryConversationQueries(repository).ListAsync(10, CancellationToken.None);

        list.Select(item => item.Id).Should().Equal(newer.Id, older.Id);
        list[0].MessageCount.Should().Be(2);
    }
}

using FluentAssertions;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Domain.Conversations;
using VocalEye.Infrastructure.Persistence;
using Xunit;

namespace VocalEye.UnitTests;

public class ConversationTitleTests
{
    [Fact]
    public void Rename_TrimsAndPutsTheTitleOnOneLine()
    {
        Conversation conversation = Conversation.Create(Guid.NewGuid());

        conversation.Rename("  Trip   plan\r\nfor May  ");

        conversation.Title.Should().Be("Trip plan for May");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_BlankTitle_Throws(string title)
    {
        Conversation conversation = Conversation.Create(Guid.NewGuid());

        Action act = () => conversation.Rename(title);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_TitleLongerThanTheLimit_Throws()
    {
        Conversation conversation = Conversation.Create(Guid.NewGuid());

        Action act = () => conversation.Rename(new string('a', Conversation.MaximumTitleLength + 1));

        act.Should().Throw<ArgumentException>().WithMessage("*100*");
    }

    [Fact]
    public void SetTitleIfMissing_LongMessage_IsShortenedWithAnEllipsis()
    {
        Conversation conversation = Conversation.Create(Guid.NewGuid());

        conversation.SetTitleIfMissing(new string('x', 200));

        conversation.Title.Should().HaveLength(60).And.EndWith("…");
    }

    [Fact]
    public void SetTitleIfMissing_AlreadyTitled_KeepsTheUsersTitle()
    {
        Conversation conversation = Conversation.Create(Guid.NewGuid());
        conversation.Rename("My title");

        conversation.SetTitleIfMissing("something else");

        conversation.Title.Should().Be("My title");
    }

    [Fact]
    public async Task SaveAsync_NewConversation_TitleComesFromTheFirstUserMessage()
    {
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationStore store = new ConversationStore(repository);
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, "Write a report about solar panels"));
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, "Sure."));

        await store.SaveAsync(state, CancellationToken.None);

        IReadOnlyList<ConversationSummaryDto> list = await new InMemoryConversationQueries(repository).ListAsync(10, CancellationToken.None);
        list.Should().ContainSingle().Which.Title.Should().Be("Write a report about solar panels");
    }

    [Fact]
    public async Task Manager_RenameAndDelete_ReportWhetherTheConversationExisted()
    {
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationStore store = new ConversationStore(repository);
        ConversationManager manager = new ConversationManager(repository);
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, "hello"));
        await store.SaveAsync(state, CancellationToken.None);

        bool renamed = await manager.RenameAsync(state.Id, "Greetings", CancellationToken.None);
        bool renamedUnknown = await manager.RenameAsync(Guid.NewGuid(), "Nope", CancellationToken.None);
        bool deleted = await manager.DeleteAsync(state.Id, CancellationToken.None);
        bool deletedAgain = await manager.DeleteAsync(state.Id, CancellationToken.None);

        renamed.Should().BeTrue();
        renamedUnknown.Should().BeFalse();
        deleted.Should().BeTrue();
        deletedAgain.Should().BeFalse();
    }
}

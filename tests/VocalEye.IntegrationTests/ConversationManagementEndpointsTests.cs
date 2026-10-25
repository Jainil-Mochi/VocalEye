using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Contracts.Chat;
using Xunit;

namespace VocalEye.IntegrationTests;

public class ConversationManagementEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ConversationManagementEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Patch_RenamesTheConversation_AndTheListShowsTheNewTitle()
    {
        Guid id = await SeedAsync("original question");
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PatchAsJsonAsync($"/api/conversations/{id}", new RenameConversationDto { Title = "Better title" });
        List<ConversationListItemDto>? list = await client.GetFromJsonAsync<List<ConversationListItemDto>>("/api/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        list!.Single(item => item.ConversationId == id).Title.Should().Be("Better title");
    }

    [Fact]
    public async Task List_NewConversation_ShowsTheFirstMessageAsTheTitle()
    {
        Guid id = await SeedAsync("what is the capital of France");

        List<ConversationListItemDto>? list = await _factory.CreateClient().GetFromJsonAsync<List<ConversationListItemDto>>("/api/conversations");

        list!.Single(item => item.ConversationId == id).Title.Should().Be("what is the capital of France");
    }

    [Fact]
    public async Task Patch_BlankTitle_Returns400()
    {
        Guid id = await SeedAsync("hello");

        HttpResponseMessage response = await _factory.CreateClient()
            .PatchAsJsonAsync($"/api/conversations/{id}", new RenameConversationDto { Title = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_UnknownConversation_Returns404()
    {
        HttpResponseMessage response = await _factory.CreateClient()
            .PatchAsJsonAsync($"/api/conversations/{Guid.NewGuid()}", new RenameConversationDto { Title = "Anything" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesTheConversation()
    {
        Guid id = await SeedAsync("to be deleted");
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage deleted = await client.DeleteAsync($"/api/conversations/{id}");
        HttpResponseMessage messages = await client.GetAsync($"/api/conversations/{id}/messages");
        HttpResponseMessage deletedAgain = await client.DeleteAsync($"/api/conversations/{id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        messages.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deletedAgain.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedAsync(string firstMessage)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IConversationStore store = scope.ServiceProvider.GetRequiredService<IConversationStore>();
        ConversationState state = new ConversationState { Id = Guid.NewGuid() };
        state.Messages.Add(new ChatMessage(ChatRole.User, firstMessage));
        state.Messages.Add(new ChatMessage(ChatRole.Assistant, "reply"));
        await store.SaveAsync(state, CancellationToken.None);

        return state.Id;
    }
}

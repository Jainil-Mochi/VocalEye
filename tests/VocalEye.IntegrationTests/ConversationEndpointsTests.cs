using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VocalEye.Contracts.Chat;
using Xunit;

namespace VocalEye.IntegrationTests;

public class ConversationEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ConversationEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConversations_NothingStored_ReturnsEmptyList()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<ConversationListItemDto>? items = await response.Content.ReadFromJsonAsync<List<ConversationListItemDto>>();
        items.Should().NotBeNull();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_UnknownConversation_Returns404()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync($"/api/conversations/{Guid.NewGuid()}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

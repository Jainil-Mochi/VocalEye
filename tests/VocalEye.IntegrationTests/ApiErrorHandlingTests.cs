using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace VocalEye.IntegrationTests;

/// <summary>Verifies the global exception middleware turns faults into problem responses.</summary>
public class ApiErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiErrorHandlingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostChat_BodyIsNotAJsonObject_Returns400Problem()
    {
        using StringContent body = new StringContent("\"just text\"", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _factory.CreateClient().PostAsync("/api/chat", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PostChat_EmptyMessage_Returns400Problem()
    {
        using StringContent body = new StringContent("""{ "message": "" }""", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _factory.CreateClient().PostAsync("/api/chat", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostChatStream_EmptyMessage_Returns400ProblemBeforeTheStreamStarts()
    {
        using StringContent body = new StringContent("""{ "message": "" }""", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _factory.CreateClient().PostAsync("/api/chat/stream", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetModels_ReturnsOkJson()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/models");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}

using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace VocalEye.IntegrationTests;

public class AccessTokenTests : IClassFixture<AccessTokenApiFactory>
{
    private readonly AccessTokenApiFactory _factory;

    public AccessTokenTests(AccessTokenApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApiCall_WithoutToken_Returns401WithBearerChallenge()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/models");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ApiCall_WithWrongToken_Returns401()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-the-right-token-at-all");

        HttpResponseMessage response = await client.GetAsync("/api/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiCall_WithWrongScheme_Returns401()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", AccessTokenApiFactory.Token);

        HttpResponseMessage response = await client.GetAsync("/api/models");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Bearer")]
    [InlineData("bearer")]
    public async Task ApiCall_WithCorrectToken_Succeeds(string scheme)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, AccessTokenApiFactory.Token);

        HttpResponseMessage response = await client.GetAsync("/api/models");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChatStream_WithoutToken_IsRefusedBeforeAnythingRuns()
    {
        using StringContent body = new StringContent("""{ "message": "hi" }""", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _factory.CreateClient().PostAsync("/api/chat/stream", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_WithoutToken_StaysOpenSoTheAppCanWaitForStartup()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

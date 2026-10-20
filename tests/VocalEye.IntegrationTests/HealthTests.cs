using System.Net;
using FluentAssertions;
using Xunit;

namespace VocalEye.IntegrationTests;

public class HealthTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public HealthTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

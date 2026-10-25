using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using VocalEye.Contracts.Info;
using Xunit;

namespace VocalEye.IntegrationTests;

public class InfoEndpointTests : IClassFixture<TestApiFactory>, IClassFixture<AccessTokenApiFactory>
{
    private readonly TestApiFactory _factory;
    private readonly AccessTokenApiFactory _protectedFactory;

    public InfoEndpointTests(TestApiFactory factory, AccessTokenApiFactory protectedFactory)
    {
        _factory = factory;
        _protectedFactory = protectedFactory;
    }

    [Fact]
    public async Task GetInfo_ReturnsHowTheBackendIsSetUp()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/info");
        AppInfoDto? info = await response.Content.ReadFromJsonAsync<AppInfoDto>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        info.Should().NotBeNull();
        info!.Version.Should().NotBeNullOrWhiteSpace();
        info.OperatingSystem.Should().NotBeNullOrWhiteSpace();
        info.PersistenceProvider.Should().Be("InMemory");
        info.AccessTokenRequired.Should().BeFalse();
        info.OutputDirectory.Should().NotBeNullOrWhiteSpace();
        info.ConfiguredProviders.Count.Should().BeLessThanOrEqualTo(info.ModelCount);
    }

    [Fact]
    public async Task GetInfo_ListsOnlyToolsThatNeedApproval()
    {
        AppInfoDto? info = await _factory.CreateClient().GetFromJsonAsync<AppInfoDto>("/api/info");

        info.Should().NotBeNull();
        info!.ToolsRequiringApproval.Should().Contain(new[] { "file_generator", "code_builder", "desktop_command", "send_email" });
        info.ToolsRequiringApproval.Should().NotContain("get_current_time");
    }

    [Fact]
    public async Task GetInfo_NeverContainsSecrets()
    {
        string body = await _factory.CreateClient().GetStringAsync("/api/info");

        body.Should().NotContainEquivalentOf("apikey").And.NotContainEquivalentOf("password");
    }

    [Fact]
    public async Task GetInfo_WhenATokenIsRequired_RefusesCallsWithoutIt()
    {
        HttpResponseMessage response = await _protectedFactory.CreateClient().GetAsync("/api/info");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VocalEye.IntegrationTests;

/// <summary>Starts the API with an access token set, so every /api call must prove it knows it.</summary>
public class AccessTokenApiFactory : WebApplicationFactory<Program>
{
    public const string Token = "test-token-0123456789abcdef";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Persistence:Provider", "InMemory");
        builder.UseSetting("Api:AccessToken", Token);
    }
}

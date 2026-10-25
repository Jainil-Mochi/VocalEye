using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using VocalEye.Api.Configuration;

namespace VocalEye.Api.Middleware;

/// <summary>
/// Requires "Authorization: Bearer &lt;token&gt;" on every /api call when Api:AccessToken is set. /health, Swagger and the OpenAPI
/// document are not under /api and stay open. Without it any program on the PC could call the API, and approve a tool.
/// </summary>
public class AccessTokenMiddleware
{
    private const string BearerScheme = "Bearer";

    private readonly RequestDelegate _next;
    private readonly ApiAccessOptions _options;

    public AccessTokenMiddleware(RequestDelegate next, ApiAccessOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.IsEnabled || !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        if (HasValidToken(context.Request))
        {
            await _next(context);
            return;
        }

        context.Response.Headers.WWWAuthenticate = BearerScheme;
        IResult problem = Results.Problem(
            title: "Unauthorized",
            detail: "Send the access token as 'Authorization: Bearer <token>'.",
            statusCode: StatusCodes.Status401Unauthorized);

        await problem.ExecuteAsync(context);
    }

    private bool HasValidToken(HttpRequest request)
    {
        string? header = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue? parsed))
        {
            return false;
        }

        if (!string.Equals(parsed.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(parsed.Parameter))
        {
            return false;
        }

        byte[] expected = Encoding.UTF8.GetBytes(_options.AccessToken!);
        byte[] actual = Encoding.UTF8.GetBytes(parsed.Parameter);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}

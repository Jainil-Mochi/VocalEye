using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

/// <summary>Development-only helpers. Not mapped in other environments.</summary>
public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/dev/llm-ping   body (optional): { "provider": "gemini", "model": "gemini-2.0-flash" }
        app.MapPost("/api/dev/llm-ping", PingAsync);

        return app;
    }

    private static async Task<IResult> PingAsync(
        ModelSelectionDto? requested,
        IModelCatalog catalog,
        IChatClientFactory factory,
        CancellationToken cancellationToken)
    {
        ModelSelection selection = requested is null
            ? catalog.Default
            : new ModelSelection(requested.Provider, requested.Model);

        IChatClient client = factory.Create(selection);
        ChatResponse response = await client.GetResponseAsync("Reply with exactly: pong", cancellationToken: cancellationToken);

        return Results.Ok(new { model = $"{selection.Provider}/{selection.Model}", reply = response.Text });
    }
}

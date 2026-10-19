using VocalEye.Api.Mapping;
using VocalEye.Api.Streaming;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        // Typed text and transcribed speech arrive the same way: Message + InputMode.
        app.MapPost("/api/chat", ChatAsync);

        // Same request body, but the answer streams as server-sent events: "text", then "completed" (or "error").
        app.MapPost("/api/chat/stream", StreamAsync);

        return app;
    }

    private static async Task<IResult> ChatAsync(
        ChatRequestDto body,
        IAgentOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        Guid conversationId = body.ConversationId ?? Guid.NewGuid();

        AgentReply reply = await orchestrator.RunAsync(ChatMapper.ToRequest(body, conversationId), cancellationToken);

        return Results.Ok(ChatMapper.ToResponse(reply, conversationId));
    }

    private static async Task<IResult> StreamAsync(
        ChatRequestDto body,
        IAgentOrchestrator orchestrator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        Guid conversationId = body.ConversationId ?? Guid.NewGuid();

        IAsyncEnumerator<AgentStreamEvent> events = orchestrator
            .RunStreamingAsync(ChatMapper.ToRequest(body, conversationId), cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        // Pull the first event before the response starts: validation errors and a model that refuses the
        // request then become a normal 400/502 problem response from the global middleware.
        if (!await events.MoveNextAsync())
        {
            await events.DisposeAsync();
            return Results.NoContent();
        }

        ILogger logger = loggerFactory.CreateLogger("VocalEye.Api.ChatStream");

        return TypedResults.ServerSentEvents(ChatStreamWriter.WriteAsync(events, conversationId, logger, cancellationToken));
    }
}

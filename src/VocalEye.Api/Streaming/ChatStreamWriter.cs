using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VocalEye.Api.Mapping;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Streaming;

/// <summary>
/// Turns the orchestrator's events into server-sent events: "text" (deltas), "completed" (final response), "error".
/// Once the first byte is sent the status code cannot change, so a fault here becomes an "error" event
/// instead of reaching the global exception middleware (ADR-0008, case 3).
/// </summary>
public static class ChatStreamWriter
{
    public const string TextEvent = "text";
    public const string CompletedEvent = "completed";
    public const string ErrorEvent = "error";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <param name="events">An enumerator whose first MoveNext already succeeded (so early errors were handled by the middleware).</param>
    /// <param name="conversationId">Echoed in the completed event.</param>
    /// <param name="logger">Used to log a fault that happens mid-stream.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    public static async IAsyncEnumerable<SseItem<string>> WriteAsync(
        IAsyncEnumerator<AgentStreamEvent> events,
        Guid conversationId,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            bool hasCurrent = true;
            while (hasCurrent)
            {
                yield return ToSseItem(events.Current, conversationId);

                StreamStep step = await AdvanceAsync(events);
                if (step.Error is not null)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        logger.LogError(step.Error, "Streaming chat failed after the response started.");
                        yield return ToErrorItem(step.Error);
                    }

                    yield break;
                }

                hasCurrent = step.HasNext;
            }
        }
        finally
        {
            await events.DisposeAsync();
        }
    }

    private static async Task<StreamStep> AdvanceAsync(IAsyncEnumerator<AgentStreamEvent> events)
    {
        try
        {
            bool hasNext = await events.MoveNextAsync();
            return new StreamStep { HasNext = hasNext };
        }
        catch (Exception exception)
        {
            return new StreamStep { Error = exception };
        }
    }

    private static SseItem<string> ToSseItem(AgentStreamEvent streamEvent, Guid conversationId)
    {
        if (streamEvent.Kind == AgentStreamEventKind.TextDelta)
        {
            ChatStreamTextDto text = new ChatStreamTextDto { Text = streamEvent.Text ?? string.Empty };
            return new SseItem<string>(JsonSerializer.Serialize(text, JsonOptions), TextEvent);
        }

        if (streamEvent.Reply is null)
        {
            throw new InvalidOperationException("A completed stream event must carry the reply.");
        }

        ChatResponseDto response = ChatMapper.ToResponse(streamEvent.Reply, conversationId);
        return new SseItem<string>(JsonSerializer.Serialize(response, JsonOptions), CompletedEvent);
    }

    private static SseItem<string> ToErrorItem(Exception exception)
    {
        ChatStreamErrorDto error = new ChatStreamErrorDto
        {
            Title = exception is HttpRequestException ? "Model call failed" : "Streaming failed",
            Detail = exception is HttpRequestException or ArgumentException ? exception.Message : "An unexpected error occurred.",
        };

        return new SseItem<string>(JsonSerializer.Serialize(error, JsonOptions), ErrorEvent);
    }
}

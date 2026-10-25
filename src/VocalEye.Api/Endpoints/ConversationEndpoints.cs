using VocalEye.Api.Mapping;
using VocalEye.Application.Abstractions.Memory;
using VocalEye.Application.Conversations;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

public static class ConversationEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSizeLimit = 100;

    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/conversations?take=20
        app.MapGet("/api/conversations", ListAsync);

        // GET /api/conversations/{id}/messages
        app.MapGet("/api/conversations/{conversationId:guid}/messages", GetMessagesAsync);

        // PATCH /api/conversations/{id}   body: { "title": "..." }
        app.MapPatch("/api/conversations/{conversationId:guid}", RenameAsync);

        // DELETE /api/conversations/{id}
        app.MapDelete("/api/conversations/{conversationId:guid}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> RenameAsync(
        Guid conversationId,
        RenameConversationDto body,
        IConversationManager manager,
        CancellationToken cancellationToken)
    {
        bool found = await manager.RenameAsync(conversationId, body.Title, cancellationToken);

        return found ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> DeleteAsync(
        Guid conversationId,
        IConversationManager manager,
        CancellationToken cancellationToken)
    {
        bool found = await manager.DeleteAsync(conversationId, cancellationToken);

        return found ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ListAsync(
        IConversationQueries queries,
        CancellationToken cancellationToken,
        int? take = null)
    {
        int pageSize = Math.Clamp(take ?? DefaultPageSize, 1, MaximumPageSizeLimit);

        IReadOnlyList<ConversationSummaryDto> summaries = await queries.ListAsync(pageSize, cancellationToken);

        return Results.Ok(summaries.Select(ConversationMapper.ToListItem).ToList());
    }

    private static async Task<IResult> GetMessagesAsync(
        Guid conversationId,
        IConversationQueries queries,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StoredMessageDto>? messages = await queries.GetMessagesAsync(conversationId, cancellationToken);
        if (messages is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(messages.Select(ConversationMapper.ToMessage).ToList());
    }
}

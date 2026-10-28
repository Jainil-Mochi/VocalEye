using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Application.Conversations;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Mapping;

public static class ConversationMapper
{
    public static ConversationListItemDto ToListItem(ConversationSummaryDto summary)
    {
        bool hasModel = summary.ModelProvider is not null && summary.ModelName is not null;

        return new ConversationListItemDto
        {
            ConversationId = summary.Id,
            Title = summary.Title,
            Model = hasModel
                ? new ModelSelectionDto { Provider = summary.ModelProvider!, Model = summary.ModelName! }
                : null,
            UpdatedAt = summary.UpdatedAt,
            MessageCount = summary.MessageCount,
        };
    }

    /// <summary>Maps the whole history at once, because a tool result needs the name and arguments of the call it answers.</summary>
    public static List<ConversationMessageDto> ToMessages(IReadOnlyList<StoredMessageDto> stored)
    {
        List<(StoredMessageDto Stored, ChatMessage Message)> parsed = stored
            .Select(item => (item, ChatMessageMapper.FromJson(item.ContentJson)))
            .ToList();

        Dictionary<string, FunctionCallContent> calls = parsed
            .SelectMany(item => item.Message.Contents)
            .OfType<FunctionCallContent>()
            .GroupBy(call => call.CallId)
            .ToDictionary(group => group.Key, group => group.First());

        return parsed.Select(item => ToMessage(item.Stored, item.Message, calls)).ToList();
    }

    private static ConversationMessageDto ToMessage(
        StoredMessageDto stored,
        ChatMessage message,
        Dictionary<string, FunctionCallContent> calls)
    {
        return new ConversationMessageDto
        {
            Sequence = stored.Sequence,
            Role = stored.Role.ToString().ToLowerInvariant(),
            Text = message.Text,
            ToolNames = message.Contents.OfType<FunctionCallContent>().Select(call => call.Name).ToList(),
            ToolResults = message.Contents
                .OfType<FunctionResultContent>()
                .Select(result => ToToolResult(result, calls))
                .ToList(),
        };
    }

    private static ToolCallDto ToToolResult(FunctionResultContent result, Dictionary<string, FunctionCallContent> calls)
    {
        calls.TryGetValue(result.CallId, out FunctionCallContent? call);
        string output = result.Result?.ToString() ?? string.Empty;
        bool failed = output.StartsWith(ToolResultText.ErrorPrefix, StringComparison.Ordinal);
        string text = failed ? output[ToolResultText.ErrorPrefix.Length..] : output;
        IReadOnlyList<ToolArtifact> artifacts = Array.Empty<ToolArtifact>();
        if (!failed)
        {
            (text, artifacts) = ToolResultText.Split(output);
        }

        return new ToolCallDto
        {
            Name = call?.Name ?? "unknown",
            ArgumentsJson = ToolCallArguments.ToJson(call),
            Success = !failed,
            Output = text,
            Artifacts = artifacts.Select(artifact => new ToolArtifactDto { Kind = artifact.Kind, Value = artifact.Value }).ToList(),
        };
    }
}

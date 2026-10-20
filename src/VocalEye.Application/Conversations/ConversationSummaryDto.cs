namespace VocalEye.Application.Conversations;

/// <summary>A thin view of a conversation for lists (no messages loaded).</summary>
public class ConversationSummaryDto
{
    public Guid Id { get; set; }

    public string? ModelProvider { get; set; }

    public string? ModelName { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int MessageCount { get; set; }
}

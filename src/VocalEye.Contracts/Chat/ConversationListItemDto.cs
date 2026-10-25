namespace VocalEye.Contracts.Chat;

public class ConversationListItemDto
{
    public Guid ConversationId { get; set; }

    /// <summary>The user's title, or the start of the first message. Null only for a conversation that has no messages.</summary>
    public string? Title { get; set; }

    public ModelSelectionDto? Model { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int MessageCount { get; set; }
}

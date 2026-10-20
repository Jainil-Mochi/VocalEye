namespace VocalEye.Contracts.Chat;

public class ConversationListItemDto
{
    public Guid ConversationId { get; set; }

    public ModelSelectionDto? Model { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int MessageCount { get; set; }
}

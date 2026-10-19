namespace VocalEye.Contracts.Chat;

public class ChatRequestDto
{
    public Guid? ConversationId { get; set; }

    /// <summary>Typed or transcribed text. May be null only when sending <see cref="Approvals"/>.</summary>
    public string? Message { get; set; }

    public string InputMode { get; set; } = InputModes.Text;

    /// <summary>Optional per-request model override; may change between messages in one conversation.</summary>
    public ModelSelectionDto? Model { get; set; }

    public List<ApprovalDecisionDto>? Approvals { get; set; }
}

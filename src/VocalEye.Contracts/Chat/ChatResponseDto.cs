namespace VocalEye.Contracts.Chat;

public class ChatResponseDto
{
    public Guid ConversationId { get; set; }

    public string Reply { get; set; } = string.Empty;

    public ModelSelectionDto ModelUsed { get; set; } = new ModelSelectionDto();

    public List<ToolCallDto> ToolCalls { get; set; } = new List<ToolCallDto>();

    public List<PendingApprovalDto> PendingApprovals { get; set; } = new List<PendingApprovalDto>();
}

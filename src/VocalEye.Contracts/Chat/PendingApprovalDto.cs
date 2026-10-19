namespace VocalEye.Contracts.Chat;

public class PendingApprovalDto
{
    public string ApprovalId { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = "{}";
}

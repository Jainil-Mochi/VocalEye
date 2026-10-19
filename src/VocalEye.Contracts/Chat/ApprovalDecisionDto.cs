namespace VocalEye.Contracts.Chat;

public class ApprovalDecisionDto
{
    public string ApprovalId { get; set; } = string.Empty;

    public bool Approved { get; set; }
}

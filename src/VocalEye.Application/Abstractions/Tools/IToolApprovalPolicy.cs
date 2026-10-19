namespace VocalEye.Application.Abstractions.Tools;

/// <summary>Decides which tools must pause for user approval before they run.</summary>
public interface IToolApprovalPolicy
{
    bool RequiresApproval(IAgentTool tool);
}

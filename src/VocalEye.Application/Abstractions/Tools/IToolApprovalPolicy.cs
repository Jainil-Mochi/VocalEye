namespace VocalEye.Application.Abstractions.Tools;

/// <summary>
/// Decides which tools must pause for user approval. The Agents module wraps those tools in the Agent Framework's
/// approval mechanism; the client then shows the request and returns an <c>ApprovalDecision</c>.
/// </summary>
public interface IToolApprovalPolicy
{
    bool RequiresApproval(IAgentTool tool);
}

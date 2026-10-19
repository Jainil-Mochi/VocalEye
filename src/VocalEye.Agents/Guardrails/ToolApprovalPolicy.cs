using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Guardrails;

/// <summary>Tools whose risk level is in the configured set must be approved by the user before they run.</summary>
public sealed class ToolApprovalPolicy : IToolApprovalPolicy
{
    private readonly HashSet<ToolRiskLevel> _risks;

    public ToolApprovalPolicy(IEnumerable<ToolRiskLevel> requireApprovalFor)
    {
        _risks = new HashSet<ToolRiskLevel>(requireApprovalFor);
    }

    public bool RequiresApproval(IAgentTool tool)
    {
        return _risks.Contains(tool.Risk);
    }
}

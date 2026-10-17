using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Guardrails;

/// <summary>Tools whose risk level is in the configured set must be approved by the user before they run.</summary>
public sealed class ToolApprovalPolicy(IEnumerable<ToolRiskLevel> requireApprovalFor) : IToolApprovalPolicy
{
    private readonly HashSet<ToolRiskLevel> _risks = [.. requireApprovalFor];

    public bool RequiresApproval(IAgentTool tool) => _risks.Contains(tool.Risk);
}

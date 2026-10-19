using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Tooling;

public static class AgentToolFactory
{
    /// <summary>Adapts every registered tool; wraps those the policy flags so the run pauses for user approval.</summary>
    public static List<AITool> Create(IToolRegistry registry, IToolApprovalPolicy policy)
    {
        List<AITool> tools = new List<AITool>();

        foreach (IAgentTool tool in registry.All)
        {
            AIFunction function = new AgentToolAdapter(tool);
            tools.Add(policy.RequiresApproval(tool) ? new ApprovalRequiredAIFunction(function) : function);
        }

        return tools;
    }
}

using Microsoft.Extensions.DependencyInjection;
using VocalEye.Agents.Guardrails;
using VocalEye.Agents.Orchestration;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents;

public static class DependencyInjection
{
    /// <param name="requireApprovalFor">Tool risk levels that must be approved by the user before running.</param>
    public static IServiceCollection AddAgents(this IServiceCollection services, IEnumerable<ToolRiskLevel> requireApprovalFor)
    {
        services.AddSingleton<IToolApprovalPolicy>(new ToolApprovalPolicy(requireApprovalFor.ToArray()));
        services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
        return services;
    }
}

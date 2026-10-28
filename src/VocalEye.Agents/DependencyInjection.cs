using Microsoft.Extensions.DependencyInjection;
using VocalEye.Agents.Guardrails;
using VocalEye.Agents.Orchestration;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents;

public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    /// <param name="requireApprovalFor">Tool risk levels that must be approved by the user before running.</param>
    /// <param name="options">Limits for one turn, such as the number of tool rounds.</param>
    public static IServiceCollection AddAgents(this IServiceCollection services, IEnumerable<ToolRiskLevel> requireApprovalFor, AgentOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IToolApprovalPolicy>(new ToolApprovalPolicy(requireApprovalFor.ToArray()));
        // Scoped: the orchestrator uses the conversation store, which uses a per-request database context.
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        return services;
    }
}

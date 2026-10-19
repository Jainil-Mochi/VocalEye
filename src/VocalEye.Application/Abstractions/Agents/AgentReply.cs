using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Application.Abstractions.Agents;

public sealed record AgentReply(
    string Text,
    ModelSelection ModelUsed,
    IReadOnlyList<ToolInvocation> ToolCalls,
    IReadOnlyList<PendingApproval> PendingApprovals);

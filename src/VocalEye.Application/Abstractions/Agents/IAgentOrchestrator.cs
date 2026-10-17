using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Application.Abstractions.Agents;

/// <summary>How the text was produced. The agent treats both identically; this is metadata for logging, UX and evals.</summary>
public enum InputMode { Text, Voice }

/// <summary>The user's answer to a pending tool approval.</summary>
public sealed record ApprovalDecision(string ApprovalId, bool Approved);

/// <summary>
/// One turn. Supply <paramref name="UserInput"/> (typed or transcribed speech), <paramref name="Approvals"/>
/// (resuming a paused run), or both. <paramref name="Model"/> null = conversation's last model, else catalog default.
/// </summary>
public sealed record AgentRequest(
    Guid ConversationId,
    string? UserInput,
    InputMode Mode = InputMode.Text,
    ModelSelection? Model = null,
    IReadOnlyList<ApprovalDecision>? Approvals = null);

public sealed record ToolInvocation(string Name, string ArgumentsJson, ToolResult Result);

/// <summary>A tool call the agent wants to make but may not run until the user approves.</summary>
public sealed record PendingApproval(string ApprovalId, string ToolName, string ArgumentsJson);

public sealed record AgentReply(
    string Text,
    ModelSelection ModelUsed,
    IReadOnlyList<ToolInvocation> ToolCalls,
    IReadOnlyList<PendingApproval> PendingApprovals);

public interface IAgentOrchestrator
{
    /// <exception cref="ArgumentException">Nothing to do, or unknown/unavailable model.</exception>
    /// <exception cref="InvalidOperationException">Approval id does not match a pending request.</exception>
    Task<AgentReply> RunAsync(AgentRequest request, CancellationToken cancellationToken);
}

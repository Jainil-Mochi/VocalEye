using VocalEye.Application.Abstractions.Llm;

namespace VocalEye.Application.Abstractions.Agents;

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

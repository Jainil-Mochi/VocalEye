namespace VocalEye.Application.Abstractions.Agents;

/// <summary>The user's answer to a pending tool approval.</summary>
public sealed record ApprovalDecision(string ApprovalId, bool Approved);

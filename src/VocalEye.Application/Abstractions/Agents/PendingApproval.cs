namespace VocalEye.Application.Abstractions.Agents;

/// <summary>A tool call the agent wants to make but may not run until the user approves.</summary>
public sealed record PendingApproval(string ApprovalId, string ToolName, string ArgumentsJson);

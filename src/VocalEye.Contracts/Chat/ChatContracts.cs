using System;
using System.Collections.Generic;

namespace VocalEye.Contracts.Chat;

/// <summary>"text" (typed) or "voice" (transcribed on the client). Server behaviour is identical.</summary>
public static class InputModes
{
    public const string Text = "text";
    public const string Voice = "voice";
}

public class ModelSelectionDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class ApprovalDecisionDto
{
    public string ApprovalId { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

/// <summary>
/// Chat request payload.
/// </summary>
public class ChatRequest
{
    public Guid? ConversationId { get; set; }

    /// <summary>Typed or transcribed text. Null only when sending Approvals.</summary>
    public string? Message { get; set; }

    public string InputMode { get; set; } = InputModes.Text;

    /// <summary>Optional per-request model override; may change between messages in one conversation.</summary>
    public ModelSelectionDto? Model { get; set; }

    public IReadOnlyList<ApprovalDecisionDto>? Approvals { get; set; }
}

public class ToolCallDto
{
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
}

public class PendingApprovalDto
{
    public string ApprovalId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}

public class ChatResponse
{
    public Guid ConversationId { get; set; }
    public string Reply { get; set; } = string.Empty;
    public ModelSelectionDto ModelUsed { get; set; } = new ModelSelectionDto();
    public IReadOnlyList<ToolCallDto> ToolCalls { get; set; } = Array.Empty<ToolCallDto>();
    public IReadOnlyList<PendingApprovalDto> PendingApprovals { get; set; } = Array.Empty<PendingApprovalDto>();
}

/// <summary>Item returned by GET /api/models (drives the model picker in every client).</summary>
public class ModelDto
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool SupportsTools { get; set; }
    public bool SupportsStreaming { get; set; }
    public bool SupportsVision { get; set; }
    public bool IsDefault { get; set; }
}

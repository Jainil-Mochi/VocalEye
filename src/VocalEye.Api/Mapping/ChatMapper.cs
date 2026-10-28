using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Mapping;

/// <summary>Translates between the public DTOs and the application's internal request/reply models.</summary>
public static class ChatMapper
{
    public static AgentRequest ToRequest(ChatRequestDto dto, Guid conversationId)
    {
        InputMode mode = string.Equals(dto.InputMode, InputModes.Voice, StringComparison.OrdinalIgnoreCase)
            ? InputMode.Voice
            : InputMode.Text;

        ModelSelection? model = dto.Model is null
            ? null
            : new ModelSelection(dto.Model.Provider, dto.Model.Model);

        List<ApprovalDecision>? approvals = dto.Approvals?
            .Select(approval => new ApprovalDecision(approval.ApprovalId, approval.Approved))
            .ToList();

        return new AgentRequest(conversationId, dto.Message, mode, model, approvals);
    }

    public static ChatResponseDto ToResponse(AgentReply reply, Guid conversationId)
    {
        return new ChatResponseDto
        {
            ConversationId = conversationId,
            Reply = reply.Text,
            ModelUsed = new ModelSelectionDto
            {
                Provider = reply.ModelUsed.Provider,
                Model = reply.ModelUsed.Model,
            },
            ToolCalls = reply.ToolCalls.Select(ToToolCall).ToList(),
            PendingApprovals = reply.PendingApprovals.Select(ToPendingApproval).ToList(),
        };
    }

    private static ToolCallDto ToToolCall(ToolInvocation invocation)
    {
        return new ToolCallDto
        {
            Name = invocation.Name,
            ArgumentsJson = invocation.ArgumentsJson,
            Success = invocation.Result.Success,
            Output = invocation.Result.Success ? invocation.Result.Output : invocation.Result.Error ?? string.Empty,
            Artifacts = (invocation.Result.Artifacts ?? Array.Empty<ToolArtifact>())
                .Select(artifact => new ToolArtifactDto { Kind = artifact.Kind, Value = artifact.Value })
                .ToList(),
        };
    }

    public static PendingApprovalDto ToPendingApproval(PendingApproval approval)
    {
        return new PendingApprovalDto
        {
            ApprovalId = approval.ApprovalId,
            ToolName = approval.ToolName,
            ArgumentsJson = approval.ArgumentsJson,
        };
    }
}

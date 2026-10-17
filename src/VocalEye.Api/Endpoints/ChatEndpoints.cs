using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VocalEye.Application.Abstractions.Agents;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Contracts.Chat;

namespace VocalEye.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        // Typed text and transcribed speech arrive the same way: Message + InputMode.
        app.MapPost("/api/chat", async (ChatRequest body, IAgentOrchestrator orchestrator, CancellationToken ct) =>
        {
            var conversationId = body.ConversationId ?? Guid.NewGuid();

            var mode = string.Equals(body.InputMode, InputModes.Voice, StringComparison.OrdinalIgnoreCase)
                ? InputMode.Voice
                : InputMode.Text;

            var model = body.Model is null ? null : new ModelSelection(body.Model.Provider, body.Model.Model);
            var approvals = body.Approvals?.Select(a => new ApprovalDecision(a.ApprovalId, a.Approved)).ToList();

            try
            {
                var reply = await orchestrator.RunAsync(
                    new AgentRequest(conversationId, body.Message, mode, model, approvals), ct);

                return Results.Ok(new ChatResponse
                {
                    ConversationId = conversationId,
                    Reply = reply.Text,
                    ModelUsed = new ModelSelectionDto
                    {
                        Provider = reply.ModelUsed.Provider,
                        Model = reply.ModelUsed.Model
                    },
                    ToolCalls = reply.ToolCalls.Select(t => new ToolCallDto
                    {
                        Name = t.Name,
                        ArgumentsJson = t.ArgumentsJson,
                        Success = t.Result.Success,
                        Output = t.Result.Success ? t.Result.Output : (t.Result.Error ?? "")
                    }).ToList(),
                    PendingApprovals = reply.PendingApprovals.Select(p => new PendingApprovalDto
                    {
                        ApprovalId = p.ApprovalId,
                        ToolName = p.ToolName,
                        ArgumentsJson = p.ArgumentsJson
                    }).ToList()
                });
            }
            catch (ModelCallFailedException ex)
            {
                return Results.Problem(title: "Model call failed", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.Problem(title: "Invalid request", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Results.Problem(title: "Model call failed", detail: ex.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        });

        return app;
    }
}

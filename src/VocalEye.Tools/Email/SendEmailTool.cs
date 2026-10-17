using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.Email;

/// <summary>Port of legacy/python-voice-assistant/modules/llm_tools/email_sender.py. Not implemented yet (Phase 1).</summary>
public sealed class SendEmailTool : IAgentTool
{
    public string Name => "send_email";
    public string Description => "Compose and send an email.";
    public ToolRiskLevel Risk => ToolRiskLevel.Destructive;
    public string ParametersJsonSchema => """
        {"type":"object","properties":{"receiver_email":{"type":"string"},"subject":{"type":"string"},"topic":{"type":"string"}},"required":["receiver_email","subject","topic"]}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        => Task.FromResult(ToolResult.Fail("Not ported yet."));
}

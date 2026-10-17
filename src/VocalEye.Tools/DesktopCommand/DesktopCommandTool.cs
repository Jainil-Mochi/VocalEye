using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.DesktopCommand;

/// <summary>Port of legacy/python-voice-assistant/modules/llm_tools/generate_os_command.py. Not implemented yet (Phase 1).</summary>
public sealed class DesktopCommandTool : IAgentTool
{
    public string Name => "desktop_command";
    public string Description => "Perform a desktop task such as opening an app or URL.";
    public ToolRiskLevel Risk => ToolRiskLevel.Destructive;
    public string ParametersJsonSchema => """
        {"type":"object","properties":{"user_text":{"type":"string"}},"required":["user_text"]}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        => Task.FromResult(ToolResult.Fail("Not ported yet."));
}

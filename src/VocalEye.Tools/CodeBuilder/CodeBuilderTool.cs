using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.CodeBuilder;

/// <summary>Port of legacy/python-voice-assistant/modules/llm_tools/code_builder.py. Not implemented yet (Phase 1).</summary>
public sealed class CodeBuilderTool : IAgentTool
{
    public string Name => "code_builder";
    public string Description => "Generate a project skeleton from a natural-language description.";
    public ToolRiskLevel Risk => ToolRiskLevel.Write;
    public string ParametersJsonSchema => """
        {"type":"object","properties":{"user_text":{"type":"string"}},"required":["user_text"]}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        => Task.FromResult(ToolResult.Fail("Not ported yet."));
}

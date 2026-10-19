using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.FileGeneration;

/// <summary>Port of legacy/python-voice-assistant/modules/llm_tools/create_file.py. Not implemented yet (Phase 1).</summary>
public sealed class FileGenerationTool : IAgentTool
{
    public string Name => "file_generator";

    public string Description => "Generate content about a topic and save it as txt, pdf, docx or pptx.";

    public ToolRiskLevel Risk => ToolRiskLevel.Write;

    public string ParametersJsonSchema => """
        {"type":"object","properties":{"topic":{"type":"string"},"filetype":{"type":"string","enum":["txt","pdf","docx","pptx"]}},"required":["topic","filetype"]}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        return Task.FromResult(ToolResult.Fail("Not ported yet."));
    }
}

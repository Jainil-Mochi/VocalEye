using System.Text.Json;

namespace VocalEye.Application.Abstractions.Tools;

/// <summary>One capability the agent may call.</summary>
public interface IAgentTool
{
    string Name { get; }

    string Description { get; }

    /// <summary>JSON Schema (as a string) for the arguments object. Must declare at least one property (Gemini rejects empty ones).</summary>
    string ParametersJsonSchema { get; }

    ToolRiskLevel Risk { get; }

    Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}

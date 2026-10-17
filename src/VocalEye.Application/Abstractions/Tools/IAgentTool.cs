using System.Text.Json;

namespace VocalEye.Application.Abstractions.Tools;

/// <summary>One capability the agent may call (the .NET equivalent of a Gemini function declaration).</summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    /// <summary>JSON Schema (as a string) describing the arguments object.</summary>
    string ParametersJsonSchema { get; }
    ToolRiskLevel Risk { get; }
    Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}

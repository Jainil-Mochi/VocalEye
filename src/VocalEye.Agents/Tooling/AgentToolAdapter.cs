using System.Text.Json;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Tooling;

/// <summary>
/// Exposes our provider-neutral <see cref="IAgentTool"/> to the Agent Framework as an <see cref="AIFunction"/>.
/// This is a tool boundary (ADR-0008): a failing tool is reported to the model as text starting with
/// <see cref="ErrorPrefix"/> so it can explain or retry, instead of aborting the whole run.
/// </summary>
public sealed class AgentToolAdapter : AIFunction
{
    public const string ErrorPrefix = ToolResultText.ErrorPrefix;

    private readonly IAgentTool _tool;
    private readonly JsonElement _schema;

    public AgentToolAdapter(IAgentTool tool)
    {
        _tool = tool;

        using JsonDocument document = JsonDocument.Parse(tool.ParametersJsonSchema);
        _schema = document.RootElement.Clone();
    }

    public override string Name => _tool.Name;

    public override string Description => _tool.Description;

    public override JsonElement JsonSchema => _schema;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        try
        {
            Dictionary<string, object?> values = arguments.ToDictionary(pair => pair.Key, pair => pair.Value);
            JsonElement argumentsJson = JsonSerializer.SerializeToElement(values);
            ToolResult result = await _tool.ExecuteAsync(argumentsJson, cancellationToken);

            return result.Success ? ToolResultText.Append(result.Output, result.Artifacts) : ErrorPrefix + result.Error;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return ErrorPrefix + exception.Message;
        }
    }
}

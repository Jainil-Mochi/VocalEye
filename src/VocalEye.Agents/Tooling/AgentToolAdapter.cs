using System.Text.Json;
using Microsoft.Extensions.AI;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Agents.Tooling;

/// <summary>
/// Exposes our provider-neutral <see cref="IAgentTool"/> to the Agent Framework as an <see cref="AIFunction"/>.
/// Failures are returned to the model as text starting with <see cref="ErrorPrefix"/> so it can explain or retry.
/// </summary>
public sealed class AgentToolAdapter : AIFunction
{
    public const string ErrorPrefix = "Error: ";

    private readonly IAgentTool _tool;
    private readonly JsonElement _schema;

    public AgentToolAdapter(IAgentTool tool)
    {
        _tool = tool;
        using var document = JsonDocument.Parse(tool.ParametersJsonSchema);
        _schema = document.RootElement.Clone();
    }

    public override string Name => _tool.Name;
    public override string Description => _tool.Description;
    public override JsonElement JsonSchema => _schema;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        try
        {
            var values = arguments.ToDictionary(kv => kv.Key, kv => kv.Value);
            var args = JsonSerializer.SerializeToElement(values);
            var result = await _tool.ExecuteAsync(args, cancellationToken);
            return result.Success ? result.Output : ErrorPrefix + result.Error;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorPrefix + ex.Message;
        }
    }
}

using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.Utility;

public sealed class GetCurrentTimeTool : IAgentTool
{
    public string Name => "get_current_time";
    public string Description => "Get the current local date and time of the server.";
    public ToolRiskLevel Risk => ToolRiskLevel.ReadOnly;
    public string ParametersJsonSchema => """{"type":"object","properties":{}}""";

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        => Task.FromResult(ToolResult.Ok(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz")));
}

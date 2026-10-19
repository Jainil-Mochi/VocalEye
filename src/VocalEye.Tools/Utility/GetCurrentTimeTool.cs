using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;

namespace VocalEye.Tools.Utility;

public sealed class GetCurrentTimeTool : IAgentTool
{
    public string Name => "get_current_time";

    public string Description => "Get the current date and time, optionally in a specific time zone.";

    public ToolRiskLevel Risk => ToolRiskLevel.ReadOnly;

    // Gemini rejects tools whose schema has an empty "properties" object, so every tool declares at least one parameter.
    public string ParametersJsonSchema => """
        {"type":"object","properties":{"timezone":{"type":"string","description":"Optional time zone id such as 'Asia/Kolkata' or 'UTC'. Defaults to the server's local time zone."}}}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string? zoneId = ReadTimeZoneId(arguments);

        try
        {
            TimeZoneInfo zone = string.IsNullOrWhiteSpace(zoneId)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            DateTimeOffset now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);

            return Task.FromResult(ToolResult.Ok($"{now:yyyy-MM-dd HH:mm:ss zzz} ({zone.Id})"));
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Expected user-input failure: reported to the model as a tool result so it can ask for a valid zone.
            return Task.FromResult(ToolResult.Fail($"Unknown time zone '{zoneId}'."));
        }
    }

    private static string? ReadTimeZoneId(JsonElement arguments)
    {
        if (arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty("timezone", out JsonElement value)
            && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }
}

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace VocalEye.Application.Conversations;

public static class ToolCallArguments
{
    /// <summary>The arguments of a tool call as one JSON object text; "{}" when there are none.</summary>
    public static string ToJson(FunctionCallContent? call)
    {
        return JsonSerializer.Serialize(call?.Arguments ?? new Dictionary<string, object?>());
    }
}

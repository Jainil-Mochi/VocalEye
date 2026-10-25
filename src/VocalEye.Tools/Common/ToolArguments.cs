using System.Text.Json;

namespace VocalEye.Tools.Common;

/// <summary>Reads the arguments the model sent. Problems are reported as <see cref="ArgumentException"/> with a message the model can act on.</summary>
public static class ToolArguments
{
    public static string? GetOptionalString(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    /// <exception cref="ArgumentException">The argument is missing or blank.</exception>
    public static string GetRequiredString(JsonElement arguments, string name)
    {
        string? value = GetOptionalString(arguments, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"The '{name}' argument is required.");
        }

        return value;
    }

    /// <exception cref="ArgumentException">The argument is missing or is not an array.</exception>
    public static JsonElement GetRequiredArray(JsonElement arguments, string name)
    {
        if (arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.Array)
        {
            return value;
        }

        throw new ArgumentException($"The '{name}' argument is required and must be an array.");
    }
}

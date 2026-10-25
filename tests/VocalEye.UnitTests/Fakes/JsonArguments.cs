using System.Text.Json;

namespace VocalEye.UnitTests.Fakes;

internal static class JsonArguments
{
    /// <summary>Builds the JSON arguments object a model would send, from an anonymous object.</summary>
    public static JsonElement From(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }
}

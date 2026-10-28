using System.Text.Json;

namespace VocalEye.Application.Abstractions.Tools;

/// <summary>
/// How a tool result travels through the model and into stored history as plain text.
/// A failure is "Error: ..."; files and folders a tool made are added as a last line "[artifacts] [json]" that
/// <see cref="Split"/> removes again before a client sees the text.
/// </summary>
public static class ToolResultText
{
    public const string ErrorPrefix = "Error: ";

    private const string ArtifactMarker = "\n[artifacts] ";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static string Append(string output, IReadOnlyList<ToolArtifact>? artifacts)
    {
        if (artifacts is null || artifacts.Count == 0)
        {
            return output;
        }

        return output + ArtifactMarker + JsonSerializer.Serialize(artifacts, JsonOptions);
    }

    /// <summary>Separates the readable text from the artifacts. A line that is not valid JSON is left in the text.</summary>
    public static (string Output, IReadOnlyList<ToolArtifact> Artifacts) Split(string text)
    {
        int index = text.LastIndexOf(ArtifactMarker, StringComparison.Ordinal);
        if (index < 0)
        {
            return (text, Array.Empty<ToolArtifact>());
        }

        try
        {
            List<ToolArtifact>? artifacts = JsonSerializer.Deserialize<List<ToolArtifact>>(text[(index + ArtifactMarker.Length)..], JsonOptions);
            if (artifacts is null)
            {
                return (text, Array.Empty<ToolArtifact>());
            }

            return (text[..index], artifacts);
        }
        catch (JsonException)
        {
            return (text, Array.Empty<ToolArtifact>());
        }
    }
}

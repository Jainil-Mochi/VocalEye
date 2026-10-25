namespace VocalEye.Tools.Documents;

/// <summary>A run of text with the same formatting.</summary>
public class TextSegment
{
    public string Text { get; set; } = string.Empty;

    public bool Bold { get; set; }

    public bool Italic { get; set; }
}

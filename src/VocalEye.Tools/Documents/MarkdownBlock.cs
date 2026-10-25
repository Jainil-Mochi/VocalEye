namespace VocalEye.Tools.Documents;

/// <summary>One line of the document: a heading, a bullet or a paragraph.</summary>
public class MarkdownBlock
{
    public MarkdownBlockKind Kind { get; set; }

    public List<TextSegment> Segments { get; set; } = new List<TextSegment>();
}

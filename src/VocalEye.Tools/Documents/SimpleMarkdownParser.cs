using System.Text.RegularExpressions;

namespace VocalEye.Tools.Documents;

/// <summary>
/// Reads the small Markdown subset the model is told to write: "# ", "## ", "### " headings, "- " bullets,
/// **bold** and *italic*. Everything else is a paragraph. Port of the line rules in the Python txt_to_pdf/txt_to_docx.
/// </summary>
public static class SimpleMarkdownParser
{
    private static readonly Regex InlinePattern = new Regex(@"(\*\*.+?\*\*|\*.+?\*)", RegexOptions.Compiled);

    public static List<MarkdownBlock> Parse(string content)
    {
        List<MarkdownBlock> blocks = new List<MarkdownBlock>();

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("```", StringComparison.Ordinal))
            {
                continue;
            }

            blocks.Add(ParseLine(line));
        }

        return blocks;
    }

    private static MarkdownBlock ParseLine(string line)
    {
        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            return Create(MarkdownBlockKind.Heading3, line.Substring(4));
        }

        if (line.StartsWith("## ", StringComparison.Ordinal))
        {
            return Create(MarkdownBlockKind.Heading2, line.Substring(3));
        }

        if (line.StartsWith("# ", StringComparison.Ordinal))
        {
            return Create(MarkdownBlockKind.Heading1, line.Substring(2));
        }

        if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
        {
            return Create(MarkdownBlockKind.Bullet, line.Substring(2));
        }

        return Create(MarkdownBlockKind.Paragraph, line);
    }

    private static MarkdownBlock Create(MarkdownBlockKind kind, string text)
    {
        return new MarkdownBlock { Kind = kind, Segments = ParseInline(text) };
    }

    private static List<TextSegment> ParseInline(string text)
    {
        List<TextSegment> segments = new List<TextSegment>();

        foreach (string token in InlinePattern.Split(text))
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (token.Length > 4 && token.StartsWith("**", StringComparison.Ordinal) && token.EndsWith("**", StringComparison.Ordinal))
            {
                segments.Add(new TextSegment { Text = token.Substring(2, token.Length - 4), Bold = true });
            }
            else if (token.Length > 2 && token.StartsWith('*') && token.EndsWith('*'))
            {
                segments.Add(new TextSegment { Text = token.Substring(1, token.Length - 2), Italic = true });
            }
            else
            {
                segments.Add(new TextSegment { Text = token });
            }
        }

        return segments;
    }
}

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using VocalEye.Tools.Documents;

namespace VocalEye.Tools.FileGeneration;

/// <summary>
/// Writes Word files with the Open XML SDK. Headings use direct formatting (bold, larger size) and bullets use a
/// "•" prefix with a hanging indent, so no styles or numbering parts are needed.
/// </summary>
public sealed class DocxDocumentWriter : IDocumentWriter
{
    public string Extension => "docx";

    public void Write(IReadOnlyList<MarkdownBlock> blocks, string path)
    {
        using (WordprocessingDocument document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = document.AddMainDocumentPart();
            Body body = new Body();

            foreach (MarkdownBlock block in blocks)
            {
                body.Append(CreateParagraph(block));
            }

            mainPart.Document = new Document(body);
            mainPart.Document.Save();
        }
    }

    private static Paragraph CreateParagraph(MarkdownBlock block)
    {
        Paragraph paragraph = new Paragraph();
        ParagraphProperties properties = new ParagraphProperties();
        properties.Append(new SpacingBetweenLines { After = "120" });

        if (block.Kind == MarkdownBlockKind.Bullet)
        {
            properties.Append(new Indentation { Left = "720", Hanging = "360" });
        }

        paragraph.Append(properties);

        string size = GetFontSize(block.Kind);
        bool isHeading = block.Kind is MarkdownBlockKind.Heading1 or MarkdownBlockKind.Heading2 or MarkdownBlockKind.Heading3;

        if (block.Kind == MarkdownBlockKind.Bullet)
        {
            paragraph.Append(CreateRun("•  ", false, false, size));
        }

        foreach (TextSegment segment in block.Segments)
        {
            paragraph.Append(CreateRun(segment.Text, isHeading || segment.Bold, segment.Italic, size));
        }

        return paragraph;
    }

    private static Run CreateRun(string text, bool bold, bool italic, string size)
    {
        RunProperties properties = new RunProperties();

        if (bold)
        {
            properties.Append(new Bold());
        }

        if (italic)
        {
            properties.Append(new Italic());
        }

        properties.Append(new FontSize { Val = size });

        Run run = new Run();
        run.Append(properties);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    /// <summary>Word measures font size in half points.</summary>
    private static string GetFontSize(MarkdownBlockKind kind)
    {
        return kind switch
        {
            MarkdownBlockKind.Heading1 => "40",
            MarkdownBlockKind.Heading2 => "32",
            MarkdownBlockKind.Heading3 => "26",
            _ => "24",
        };
    }
}

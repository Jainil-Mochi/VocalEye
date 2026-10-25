using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VocalEye.Tools.Documents;

namespace VocalEye.Tools.FileGeneration;

/// <summary>Writes PDF files with QuestPDF (Community licence: free for individuals and small companies, see QuestPDF's licence terms).</summary>
public sealed class PdfDocumentWriter : IDocumentWriter
{
    static PdfDocumentWriter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Extension => "pdf";

    public void Write(IReadOnlyList<MarkdownBlock> blocks, string path)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(style => style.FontSize(12));
                page.Content().Column(column =>
                {
                    column.Spacing(8);

                    foreach (MarkdownBlock block in blocks)
                    {
                        column.Item().Text(text => AddBlock(text, block));
                    }
                });
            });
        }).GeneratePdf(path);
    }

    private static void AddBlock(TextDescriptor text, MarkdownBlock block)
    {
        float size = GetFontSize(block.Kind);
        bool isHeading = block.Kind is MarkdownBlockKind.Heading1 or MarkdownBlockKind.Heading2 or MarkdownBlockKind.Heading3;

        if (block.Kind == MarkdownBlockKind.Bullet)
        {
            text.Span("•  ").FontSize(size);
        }

        foreach (TextSegment segment in block.Segments)
        {
            TextSpanDescriptor span = text.Span(segment.Text).FontSize(size);

            if (isHeading || segment.Bold)
            {
                span = span.Bold();
            }

            if (segment.Italic)
            {
                span = span.Italic();
            }
        }
    }

    private static float GetFontSize(MarkdownBlockKind kind)
    {
        return kind switch
        {
            MarkdownBlockKind.Heading1 => 24,
            MarkdownBlockKind.Heading2 => 18,
            MarkdownBlockKind.Heading3 => 14,
            _ => 12,
        };
    }
}

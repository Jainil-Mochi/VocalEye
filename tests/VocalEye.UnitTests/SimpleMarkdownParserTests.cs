using FluentAssertions;
using VocalEye.Tools.Documents;
using Xunit;

namespace VocalEye.UnitTests;

public class SimpleMarkdownParserTests
{
    [Fact]
    public void Parse_Headings_AreRecognizedByLevel()
    {
        List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse("# One\n## Two\n### Three");

        blocks.Select(block => block.Kind).Should().Equal(
            MarkdownBlockKind.Heading1,
            MarkdownBlockKind.Heading2,
            MarkdownBlockKind.Heading3);
        blocks[0].Segments.Should().ContainSingle().Which.Text.Should().Be("One");
    }

    [Theory]
    [InlineData("- first")]
    [InlineData("* first")]
    public void Parse_BulletMarkers_BecomeBullets(string line)
    {
        List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse(line);

        blocks.Should().ContainSingle().Which.Kind.Should().Be(MarkdownBlockKind.Bullet);
        blocks[0].Segments.Single().Text.Should().Be("first");
    }

    [Fact]
    public void Parse_BoldAndItalic_AreSplitIntoSegments()
    {
        List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse("plain **strong** and *slanted* end");

        List<TextSegment> segments = blocks.Single().Segments;
        segments.Select(segment => segment.Text).Should().Equal("plain ", "strong", " and ", "slanted", " end");
        segments[1].Bold.Should().BeTrue();
        segments[3].Italic.Should().BeTrue();
        segments[0].Bold.Should().BeFalse();
    }

    [Fact]
    public void Parse_BlankLinesAndCodeFences_AreSkipped()
    {
        List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse("first\n\n```\ncode line\n```\n  \nlast");

        blocks.Select(block => block.Segments.Single().Text).Should().Equal("first", "code line", "last");
    }

    [Fact]
    public void Parse_WindowsLineEndings_AreHandled()
    {
        List<MarkdownBlock> blocks = SimpleMarkdownParser.Parse("# Title\r\n- item\r\n");

        blocks.Should().HaveCount(2);
        blocks[1].Segments.Single().Text.Should().Be("item");
    }
}

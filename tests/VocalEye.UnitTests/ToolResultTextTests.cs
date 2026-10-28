using FluentAssertions;
using VocalEye.Application.Abstractions.Tools;
using Xunit;

namespace VocalEye.UnitTests;

public class ToolResultTextTests
{
    [Fact]
    public void AppendThenSplit_RoundTripsTextAndArtifacts()
    {
        ToolArtifact[] artifacts =
        {
            new ToolArtifact(ToolArtifactKinds.File, @"C:\Users\me\Documents\VocalEye\documents\ai basics.pdf"),
            new ToolArtifact(ToolArtifactKinds.Folder, @"C:\Users\me\Documents\VocalEye\projects\todo"),
        };

        string text = ToolResultText.Append("Saved the PDF file.", artifacts);
        (string output, IReadOnlyList<ToolArtifact> parsed) = ToolResultText.Split(text);

        output.Should().Be("Saved the PDF file.");
        parsed.Should().Equal(artifacts);
    }

    [Fact]
    public void Append_NoArtifacts_LeavesTheTextUntouched()
    {
        ToolResultText.Append("plain", null).Should().Be("plain");
        ToolResultText.Append("plain", Array.Empty<ToolArtifact>()).Should().Be("plain");
    }

    [Fact]
    public void Split_TextWithoutMarker_ReturnsItAsIs()
    {
        (string output, IReadOnlyList<ToolArtifact> artifacts) = ToolResultText.Split("Opened https://example.com.");

        output.Should().Be("Opened https://example.com.");
        artifacts.Should().BeEmpty();
    }

    [Fact]
    public void Split_MarkerWithBrokenJson_KeepsTheWholeText()
    {
        string broken = "Done.\n[artifacts] {not json";

        (string output, IReadOnlyList<ToolArtifact> artifacts) = ToolResultText.Split(broken);

        output.Should().Be(broken);
        artifacts.Should().BeEmpty();
    }
}

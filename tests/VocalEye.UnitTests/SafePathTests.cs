using FluentAssertions;
using VocalEye.Tools.Common;
using VocalEye.UnitTests.Fakes;
using Xunit;

namespace VocalEye.UnitTests;

public class SafePathTests
{
    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("a/../../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("\\windows\\system32")]
    [InlineData("C:\\temp\\x.txt")]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_UnsafePaths_ThrowArgumentException(string path)
    {
        using TempOutputDirectory temp = new TempOutputDirectory();

        Action act = () => SafePath.Resolve(temp.Path, path);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_RelativePath_StaysInsideTheRoot()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();

        string result = SafePath.Resolve(temp.Path, "src/app/main.py");

        result.Should().StartWith(Path.GetFullPath(temp.Path));
        result.Should().EndWith("main.py");
    }

    [Theory]
    [InlineData("My: report", "My_ report")]
    [InlineData("a/b\\c", "a_b_c")]
    [InlineData("...", "fallback")]
    [InlineData("   ", "fallback")]
    [InlineData("../../evil", "_.._evil")]
    public void SanitizeName_ReplacesUnsafeCharacters(string name, string expected)
    {
        string result = SafePath.SanitizeName(name, "fallback");

        result.Should().Be(expected);
    }

    [Fact]
    public void SanitizeName_LongName_IsShortened()
    {
        string result = SafePath.SanitizeName(new string('a', 500), "fallback");

        result.Length.Should().BeLessThanOrEqualTo(80);
    }

    [Fact]
    public void MakeUniquePath_ExistingFile_AddsANumber()
    {
        using TempOutputDirectory temp = new TempOutputDirectory();
        string path = Path.Combine(temp.Path, "report.txt");
        File.WriteAllText(path, "x");

        string result = SafePath.MakeUniquePath(path);

        Path.GetFileName(result).Should().Be("report (2).txt");
    }
}

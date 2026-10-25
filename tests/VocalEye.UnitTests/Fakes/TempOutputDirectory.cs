using VocalEye.Tools.Common;

namespace VocalEye.UnitTests.Fakes;

/// <summary>A throw-away output folder for tests that write files. Deleted on dispose.</summary>
internal sealed class TempOutputDirectory : IDisposable
{
    public TempOutputDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vocaleye-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public ToolOutputOptions Options => new ToolOutputOptions { OutputDirectory = Path };

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}

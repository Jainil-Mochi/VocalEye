namespace VocalEye.Tools.Common;

/// <summary>Where tools that create files are allowed to write. Bound from configuration section "Tools:Output".</summary>
public class ToolOutputOptions
{
    /// <summary>Root folder for everything the tools create. Defaults to Documents\VocalEye.</summary>
    public string OutputDirectory { get; set; } = GetDefaultDirectory();

    private static string GetDefaultDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string baseFolder = string.IsNullOrWhiteSpace(documents) ? AppContext.BaseDirectory : documents;

        return Path.Combine(baseFolder, "VocalEye");
    }
}

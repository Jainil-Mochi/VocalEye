namespace VocalEye.Tools.DesktopCommand;

/// <summary>Settings for the desktop_command tool. Bound from configuration section "Tools:Desktop".</summary>
public class DesktopCommandOptions
{
    /// <summary>
    /// When true the tool opens links and apps on the computer that runs the API (correct while you run it locally).
    /// Set to false on a server: the tool then refuses, because a server cannot open anything on your desktop.
    /// </summary>
    public bool ExecuteLocally { get; set; } = true;

    /// <summary>
    /// Friendly name to executable. Only these apps can be opened; the model cannot pass any other program or arguments.
    /// Add your own in configuration, for example "Tools:Desktop:AllowedApps:vscode": "code".
    /// </summary>
    public Dictionary<string, string> AllowedApps { get; set; } = CreateDefaultApps();

    private static Dictionary<string, string> CreateDefaultApps()
    {
        Dictionary<string, string> apps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            apps["notepad"] = "notepad.exe";
            apps["calculator"] = "calc.exe";
            apps["paint"] = "mspaint.exe";
            apps["file explorer"] = "explorer.exe";
            apps["chrome"] = "chrome.exe";
            apps["edge"] = "msedge.exe";
            apps["word"] = "winword.exe";
            apps["excel"] = "excel.exe";
            apps["powerpoint"] = "powerpnt.exe";
        }

        return apps;
    }
}

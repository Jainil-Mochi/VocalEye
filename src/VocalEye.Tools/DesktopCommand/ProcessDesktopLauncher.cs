using System.Diagnostics;

namespace VocalEye.Tools.DesktopCommand;

/// <summary>Opens links and apps through the operating system's shell (no command line is built or parsed).</summary>
public sealed class ProcessDesktopLauncher : IDesktopLauncher
{
    public void OpenUrl(Uri url)
    {
        Start(url.AbsoluteUri);
    }

    public void OpenApp(string executable)
    {
        Start(executable);
    }

    private static void Start(string target)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo(target) { UseShellExecute = true };

        using Process? process = Process.Start(startInfo);
    }
}

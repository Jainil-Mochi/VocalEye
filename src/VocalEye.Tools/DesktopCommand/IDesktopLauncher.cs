namespace VocalEye.Tools.DesktopCommand;

/// <summary>Opens things on the desktop. A seam so the tool can be tested without starting real programs.</summary>
public interface IDesktopLauncher
{
    /// <summary>Opens a link in the default browser.</summary>
    void OpenUrl(Uri url);

    /// <summary>Starts an application. No arguments are ever passed.</summary>
    void OpenApp(string executable);
}

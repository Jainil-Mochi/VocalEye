namespace VocalEye.Contracts.Info;

/// <summary>Returned by GET /api/info: what this backend is and how it is set up. Never contains keys or passwords.</summary>
public class AppInfoDto
{
    public string Version { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>Postgres, Sqlite or InMemory.</summary>
    public string PersistenceProvider { get; set; } = string.Empty;

    /// <summary>True when every /api call must send the bearer token.</summary>
    public bool AccessTokenRequired { get; set; }

    /// <summary>Folder where file_generator and code_builder write.</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    public bool EmailConfigured { get; set; }

    /// <summary>False when desktop_command is switched off (for example on a server).</summary>
    public bool DesktopCommandEnabled { get; set; }

    /// <summary>Providers that have what they need (a key, or an endpoint for Ollama).</summary>
    public List<string> ConfiguredProviders { get; set; } = new List<string>();

    /// <summary>Number of models the user can pick. Zero means first-run setup is needed.</summary>
    public int ModelCount { get; set; }

    /// <summary>Names of tools that pause for the user's approval under the current policy.</summary>
    public List<string> ToolsRequiringApproval { get; set; } = new List<string>();
}

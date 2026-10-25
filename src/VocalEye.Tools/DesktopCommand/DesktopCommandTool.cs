using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Common;

namespace VocalEye.Tools.DesktopCommand;

/// <summary>
/// Opens a web link or an allow-listed application on this computer. Replaces the Python generate_os_command, where a
/// model wrote a terminal command that ran with shell=True. Here the model only picks one of two actions and a target;
/// no shell, no command line and no arguments are involved, so a prompt-injected answer cannot run arbitrary programs.
/// </summary>
public sealed class DesktopCommandTool : IAgentTool
{
    private const string OpenUrlAction = "open_url";
    private const string OpenAppAction = "open_app";

    private readonly DesktopCommandOptions _options;
    private readonly IDesktopLauncher _launcher;

    public DesktopCommandTool(DesktopCommandOptions options, IDesktopLauncher launcher)
    {
        _options = options;
        _launcher = launcher;
    }

    public string Name => "desktop_command";

    public string Description
    {
        get
        {
            string apps = _options.AllowedApps.Count == 0
                ? "none are configured"
                : string.Join(", ", _options.AllowedApps.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

            return "Open something on the user's computer: a web link (action open_url, target is a full http or https URL; " +
                "for a web search build a search URL such as https://www.google.com/search?q=...) or an application " +
                $"(action open_app, target is one of the allowed app names). Allowed apps: {apps}.";
        }
    }

    public ToolRiskLevel Risk => ToolRiskLevel.Write;

    public string ParametersJsonSchema => """
        {"type":"object","properties":{
            "action":{"type":"string","enum":["open_url","open_app"],"description":"What to open."},
            "target":{"type":"string","description":"For open_url a full https URL; for open_app an allowed app name."}
        },"required":["action","target"]}
        """;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string action = ToolArguments.GetRequiredString(arguments, "action").Trim().ToLowerInvariant();
        string target = ToolArguments.GetRequiredString(arguments, "target").Trim();

        if (!_options.ExecuteLocally)
        {
            return Task.FromResult(ToolResult.Fail(
                "Desktop actions are turned off on this server (Tools:Desktop:ExecuteLocally is false)."));
        }

        ToolResult result = action switch
        {
            OpenUrlAction => OpenUrl(target),
            OpenAppAction => OpenApp(target),
            _ => ToolResult.Fail($"Unknown action '{action}'. Use '{OpenUrlAction}' or '{OpenAppAction}'."),
        };

        return Task.FromResult(result);
    }

    private ToolResult OpenUrl(string target)
    {
        bool isWebAddress = Uri.TryCreate(target, UriKind.Absolute, out Uri? url)
            && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp);

        if (!isWebAddress || url is null)
        {
            return ToolResult.Fail($"'{target}' is not a valid http or https URL.");
        }

        _launcher.OpenUrl(url);
        return ToolResult.Ok($"Opened {url.AbsoluteUri} in the default browser.");
    }

    private ToolResult OpenApp(string target)
    {
        if (!_options.AllowedApps.TryGetValue(target, out string? executable))
        {
            string allowed = _options.AllowedApps.Count == 0 ? "none" : string.Join(", ", _options.AllowedApps.Keys);
            return ToolResult.Fail($"'{target}' is not an allowed app. Allowed apps: {allowed}.");
        }

        _launcher.OpenApp(executable);
        return ToolResult.Ok($"Started {target}.");
    }
}

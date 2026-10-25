using System.Reflection;
using System.Runtime.InteropServices;
using VocalEye.Api.Configuration;
using VocalEye.Application.Abstractions.Llm;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Contracts.Info;
using VocalEye.Infrastructure.Persistence;
using VocalEye.Tools.Common;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;

namespace VocalEye.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapInfoEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/info: for first-run checks and the settings screens of a client.
        app.MapGet("/api/info", GetInfo);

        return app;
    }

    private static AppInfoDto GetInfo(
        IConfiguration configuration,
        IModelCatalog catalog,
        IToolRegistry registry,
        IToolApprovalPolicy approvalPolicy,
        ToolOutputOptions outputOptions,
        DesktopCommandOptions desktopOptions,
        IEmailSender emailSender,
        ApiAccessOptions accessOptions)
    {
        IReadOnlyList<ModelInfo> models = catalog.List();

        return new AppInfoDto
        {
            Version = GetVersion(),
            OperatingSystem = RuntimeInformation.OSDescription,
            PersistenceProvider = PersistenceProviders.Resolve(configuration),
            AccessTokenRequired = accessOptions.IsEnabled,
            OutputDirectory = outputOptions.OutputDirectory,
            EmailConfigured = emailSender.IsConfigured,
            DesktopCommandEnabled = desktopOptions.ExecuteLocally,
            ConfiguredProviders = models.Select(model => model.Provider).Distinct().OrderBy(name => name).ToList(),
            ModelCount = models.Count,
            ToolsRequiringApproval = registry.All
                .Where(approvalPolicy.RequiresApproval)
                .Select(tool => tool.Name)
                .OrderBy(name => name)
                .ToList(),
        };
    }

    private static string GetVersion()
    {
        string? version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0";
        }

        // Build tools append "+<commit hash>"; clients only need the number.
        int plus = version.IndexOf('+');
        return plus < 0 ? version : version[..plus];
    }
}

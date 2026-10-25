using Microsoft.Extensions.DependencyInjection;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.CodeBuilder;
using VocalEye.Tools.Common;
using VocalEye.Tools.DesktopCommand;
using VocalEye.Tools.Email;
using VocalEye.Tools.FileGeneration;
using VocalEye.Tools.Registry;
using VocalEye.Tools.Utility;

namespace VocalEye.Tools;

public static class DependencyInjection
{
    /// <summary>Registers every agent tool and the registry the agent reads them from.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="outputOptions">Where file-writing tools may save files.</param>
    /// <param name="desktopOptions">Which desktop actions the desktop_command tool may perform.</param>
    /// <param name="emailOptions">SMTP settings for the send_email tool.</param>
    public static IServiceCollection AddTools(
        this IServiceCollection services,
        ToolOutputOptions outputOptions,
        DesktopCommandOptions desktopOptions,
        EmailOptions emailOptions)
    {
        services.AddSingleton(outputOptions);
        services.AddSingleton(desktopOptions);
        services.AddSingleton(emailOptions);
        services.AddSingleton<IDesktopLauncher, ProcessDesktopLauncher>();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        services.AddSingleton<IAgentTool, FileGenerationTool>();
        services.AddSingleton<IAgentTool, SendEmailTool>();
        services.AddSingleton<IAgentTool, DesktopCommandTool>();
        services.AddSingleton<IAgentTool, CodeBuilderTool>();
        services.AddSingleton<IAgentTool, GetCurrentTimeTool>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();

        return services;
    }
}

using System.Text.Json;
using VocalEye.Application.Abstractions.Tools;
using VocalEye.Tools.Common;

namespace VocalEye.Tools.Email;

/// <summary>
/// Sends one email. Port of the Python send_email, with the same change as the other tools (ADR-0010): the agent's model
/// writes the subject and the body, so the tool no longer calls a second model. One recipient only (no cc/bcc), which
/// keeps a prompt-injected "send this to everyone" from fanning out. Always approval-gated (Destructive).
/// </summary>
public sealed class SendEmailTool : IAgentTool
{
    private const int MaximumSubjectLength = 200;
    private const int MaximumBodyLength = 20_000;

    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;

    public SendEmailTool(IEmailSender sender, EmailOptions options)
    {
        _sender = sender;
        _options = options;
    }

    public string Name => "send_email";

    public string Description
    {
        get
        {
            string text = "Send one plain-text email to one recipient. You must write the subject and the complete body yourself " +
                "(professional, clear, 2-3 short paragraphs with a greeting and a closing).";

            return _sender.IsConfigured ? text : text + " NOTE: email is not configured on this system yet, so sending will fail.";
        }
    }

    public ToolRiskLevel Risk => ToolRiskLevel.Destructive;

    public string ParametersJsonSchema => """
        {"type":"object","properties":{
            "to":{"type":"string","description":"The recipient's email address, for example 'alex@example.com'."},
            "subject":{"type":"string","description":"The subject line, one line."},
            "body":{"type":"string","description":"The complete email text."}
        },"required":["to","subject","body"]}
        """;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        // Spoken addresses often arrive with spaces ("bob @ example.com"); the Python version removed them too.
        string to = ToolArguments.GetRequiredString(arguments, "to").Replace(" ", string.Empty);
        string subject = ToolArguments.GetRequiredString(arguments, "subject").Trim();
        string body = ToolArguments.GetRequiredString(arguments, "body");

        if (!_sender.IsConfigured)
        {
            return ToolResult.Fail("Email is not configured. Ask the user to set Tools:Email:Host, FromAddress, Username and Password in user-secrets.");
        }

        if (!System.Net.Mail.MailAddress.TryCreate(to, out System.Net.Mail.MailAddress? address) || !address.Host.Contains('.'))
        {
            return ToolResult.Fail($"'{to}' is not a valid email address.");
        }

        if (subject.Contains('\r') || subject.Contains('\n') || subject.Length > MaximumSubjectLength)
        {
            return ToolResult.Fail($"The subject must be a single line of at most {MaximumSubjectLength} characters.");
        }

        if (body.Length > MaximumBodyLength)
        {
            return ToolResult.Fail($"The body is too long ({body.Length} characters). The limit is {MaximumBodyLength}.");
        }

        if (_options.AllowedRecipientDomains.Count > 0
            && !_options.AllowedRecipientDomains.Contains(address.Host, StringComparer.OrdinalIgnoreCase))
        {
            return ToolResult.Fail($"Sending to '{address.Host}' is not allowed. Allowed domains: {string.Join(", ", _options.AllowedRecipientDomains)}.");
        }

        await _sender.SendAsync(address.Address, subject, body, cancellationToken);

        return ToolResult.Ok($"Email sent to {address.Address} with the subject \"{subject}\".");
    }
}

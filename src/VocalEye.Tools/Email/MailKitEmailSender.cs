using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VocalEye.Tools.Email;

/// <summary>Sends mail through an SMTP server with MailKit.</summary>
public sealed class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public MailKitEmailSender(EmailOptions options)
    {
        _options = options;
    }

    public bool IsConfigured => _options.IsConfigured;

    /// <exception cref="InvalidOperationException">Email is not configured.</exception>
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Email is not configured. Set Tools:Email:Host and Tools:Email:FromAddress (and Username/Password) in user-secrets.");
        }

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using SmtpClient client = new SmtpClient();
        SecureSocketOptions security = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.Host, _options.Port, security, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

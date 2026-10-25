namespace VocalEye.Tools.Email;

/// <summary>Sends one plain-text email. A seam so the tool can be tested without a mail server.</summary>
public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

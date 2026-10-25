namespace VocalEye.Tools.Email;

/// <summary>SMTP settings for the send_email tool. Bound from "Tools:Email". Keep the password in user-secrets, never in a file.</summary>
public class EmailOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary>True for port 587 (STARTTLS). For port 465 (implicit TLS) set false; the library then picks TLS on connect.</summary>
    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }

    /// <summary>For Gmail this must be an app password, not your normal password.</summary>
    public string? Password { get; set; }

    /// <summary>The address mail is sent from. Usually the same as <see cref="Username"/>.</summary>
    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "VocalEye";

    /// <summary>
    /// If not empty, mail can only be sent to these domains (for example "gmail.com"). A cheap defence against a
    /// prompt-injected message that tries to send your data to an attacker. Empty means any domain.
    /// </summary>
    public List<string> AllowedRecipientDomains { get; set; } = new List<string>();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromAddress)
        && (string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password));
}

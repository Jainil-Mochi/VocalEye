namespace VocalEye.Api.Configuration;

/// <summary>
/// Protects the API on a PC that other programs can reach. Bound from the "Api" section.
/// The desktop app creates a random token for each launch and passes it as Api:AccessToken (environment variable Api__AccessToken).
/// </summary>
public class ApiAccessOptions
{
    public const int MinimumTokenLength = 16;

    /// <summary>When empty, the API is open (convenient for Swagger and development). When set, every /api call must send it.</summary>
    public string? AccessToken { get; set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(AccessToken);

    /// <exception cref="InvalidOperationException">A token is set but too short to be safe.</exception>
    public void Validate()
    {
        if (AccessToken is null)
        {
            return;
        }

        if (AccessToken.Length > 0 && (string.IsNullOrWhiteSpace(AccessToken) || AccessToken.Length < MinimumTokenLength))
        {
            throw new InvalidOperationException(
                $"Api:AccessToken must be at least {MinimumTokenLength} characters (use a random value), or be left empty to turn the check off.");
        }
    }
}

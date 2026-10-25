using Microsoft.Extensions.Configuration;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>The values accepted by the "Persistence:Provider" setting.</summary>
public static class PersistenceProviders
{
    public const string Postgres = "Postgres";
    public const string Sqlite = "Sqlite";
    public const string InMemory = "InMemory";

    /// <summary>Reads the configured provider name; Postgres when nothing is set.</summary>
    public static string Resolve(IConfiguration configuration)
    {
        string? configured = configuration["Persistence:Provider"];

        return string.IsNullOrWhiteSpace(configured) ? Postgres : configured.Trim();
    }

    public static bool IsSqlite(IConfiguration configuration)
    {
        return string.Equals(Resolve(configuration), Sqlite, StringComparison.OrdinalIgnoreCase);
    }
}

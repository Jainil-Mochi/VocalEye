using Microsoft.Data.Sqlite;

namespace VocalEye.Infrastructure.Persistence;

/// <summary>Builds and prepares the SQLite connection string used by the desktop build.</summary>
public static class SqliteConnectionStrings
{
    private const string FileName = "vocaleye.db";

    /// <summary>Default: a file in the user's local application-data folder, for example %LocalAppData%\VocalEye\vocaleye.db.</summary>
    public static string CreateDefault()
    {
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string baseFolder = string.IsNullOrWhiteSpace(localData) ? AppContext.BaseDirectory : localData;

        return $"Data Source={Path.Combine(baseFolder, "VocalEye", FileName)}";
    }

    /// <summary>Creates the folder that will hold the database file, if the connection string points at a file.</summary>
    /// <exception cref="InvalidOperationException">The connection string is not valid for SQLite.</exception>
    public static void EnsureDirectoryExists(string connectionString)
    {
        string dataSource;
        try
        {
            dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException($"ConnectionStrings:Sqlite is not a valid SQLite connection string: {exception.Message}", exception);
        }

        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VocalEye.Infrastructure.Persistence;

public static class PersistenceInitializer
{
    /// <summary>
    /// Creates the database schema if it does not exist (no migrations yet, see ADR-0009). Called in Development, and always
    /// for SQLite, whose file is created on the user's machine. Does nothing when the in-memory provider is used.
    /// </summary>
    /// <exception cref="InvalidOperationException">The database cannot be reached or created.</exception>
    public static async Task InitializePersistenceAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        VocalEyeDbContext? context = scope.ServiceProvider.GetService<VocalEyeDbContext>();
        if (context is null)
        {
            return;
        }

        try
        {
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            await AddMissingColumnsAsync(context, cancellationToken).ConfigureAwait(false);

            if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // Write-ahead logging lets the history list be read while a reply is being saved.
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Could not open or create the database. For Postgres start it with 'docker compose up -d' and check " +
                "ConnectionStrings:Postgres; for SQLite check ConnectionStrings:Sqlite and that the folder is writable; " +
                "or set Persistence:Provider to InMemory.",
                exception);
        }
    }

    /// <summary>
    /// EnsureCreated never changes a table that already exists, so a column added in a later version is added here.
    /// A stop-gap until EF migrations replace it (see ADR-0009); keep each step safe to run twice.
    /// </summary>
    private static async Task AddMissingColumnsAsync(VocalEyeDbContext context, CancellationToken cancellationToken)
    {
        string? provider = context.Database.ProviderName;

        if (provider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            int existing = await context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Conversations') WHERE name = 'Title'")
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            if (existing == 0)
            {
                await context.Database
                    .ExecuteSqlRawAsync("ALTER TABLE \"Conversations\" ADD COLUMN \"Title\" TEXT NULL;", cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else if (provider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await context.Database
                .ExecuteSqlRawAsync("ALTER TABLE \"Conversations\" ADD COLUMN IF NOT EXISTS \"Title\" character varying(200) NULL;", cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VocalEye.Infrastructure.Persistence;

public static class PersistenceInitializer
{
    /// <summary>
    /// Development only: creates the database schema if it does not exist (no migrations yet, see ADR-0009).
    /// Does nothing when the in-memory provider is used.
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
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Could not reach or initialize the Postgres database. Start it with 'docker compose up -d' " +
                "(see docker-compose.yml), check ConnectionStrings:Postgres, or set Persistence:Provider to InMemory.",
                exception);
        }
    }
}

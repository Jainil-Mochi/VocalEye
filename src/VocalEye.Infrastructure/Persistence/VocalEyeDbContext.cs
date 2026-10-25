using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

public class VocalEyeDbContext : DbContext
{
    private const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";

    public VocalEyeDbContext(DbContextOptions<VocalEyeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VocalEyeDbContext).Assembly);

        if (Database.ProviderName == SqliteProviderName)
        {
            // SQLite has no native DateTimeOffset, so it cannot sort by one. Store the UTC ticks as a number instead.
            ValueConverter<DateTimeOffset, long> converter = new ValueConverter<DateTimeOffset, long>(
                value => value.UtcTicks,
                ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (IMutableProperty property in entityType.GetProperties().Where(item => item.ClrType == typeof(DateTimeOffset)))
                {
                    property.SetValueConverter(converter);
                }
            }
        }
    }
}

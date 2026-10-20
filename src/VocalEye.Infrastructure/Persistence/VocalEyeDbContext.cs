using Microsoft.EntityFrameworkCore;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence;

public class VocalEyeDbContext : DbContext
{
    public VocalEyeDbContext(DbContextOptions<VocalEyeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VocalEyeDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Role).HasConversion<string>().HasMaxLength(20);

        // "text", not "jsonb": jsonb reorders keys, and the polymorphic "$type" marker must stay first to deserialize.
        builder.Property(message => message.ContentJson).HasColumnType("text").IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.Sequence }).IsUnique();
    }
}

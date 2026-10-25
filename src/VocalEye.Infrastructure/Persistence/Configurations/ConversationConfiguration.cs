using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VocalEye.Domain.Conversations;

namespace VocalEye.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(conversation => conversation.Id);

        // Ids are assigned by the domain. Without this, EF would treat a new child with a preset key as already existing.
        builder.Property(conversation => conversation.Id).ValueGeneratedNever();
        builder.Property(conversation => conversation.Title).HasMaxLength(200);
        builder.Property(conversation => conversation.ModelProvider).HasMaxLength(100);
        builder.Property(conversation => conversation.ModelName).HasMaxLength(200);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(conversation => conversation.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

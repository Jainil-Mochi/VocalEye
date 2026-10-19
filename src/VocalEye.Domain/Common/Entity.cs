namespace VocalEye.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
}

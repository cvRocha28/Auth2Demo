using System;

namespace Auth2Demo.Domain.Common;

public abstract class AuditableEntity<TKey> : Entity<TKey> where TKey : notnull
{
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }

    public void MarkAsUpdated() => UpdatedAt = DateTimeOffset.UtcNow;

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        MarkAsUpdated();
    }
}

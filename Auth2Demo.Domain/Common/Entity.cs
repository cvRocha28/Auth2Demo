namespace Auth2Demo.Domain.Common;

public abstract class Entity<TKey> where TKey : notnull
{
    public TKey Id { get; set; } = default!;
}

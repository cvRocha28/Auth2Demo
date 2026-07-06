namespace Auth2Demo.Domain.Identity;

public sealed class IdentityApplicationSecret
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string SecretPrefix { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;
}

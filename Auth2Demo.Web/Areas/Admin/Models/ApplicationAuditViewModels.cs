namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ApplicationAuditListItemViewModel
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ClientType { get; set; }
    public string? ConsentType { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public sealed class ApplicationSecretAuditListItemViewModel
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SecretPrefix { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsActive => RevokedAtUtc is null && (ExpiresAtUtc is null || ExpiresAtUtc > DateTimeOffset.UtcNow);
}

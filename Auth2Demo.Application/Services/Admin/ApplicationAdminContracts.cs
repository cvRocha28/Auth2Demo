namespace Auth2Demo.Application.Services.Admin;

public sealed record ApplicationAuditItemData(
    Guid Id,
    string ClientId,
    string? DisplayName,
    string? ClientType,
    string? ConsentType,
    DateTimeOffset CreatedAt,
    Guid? CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId,
    DateTimeOffset? DeletedAt,
    Guid? DeletedByUserId,
    bool IsDeleted,
    bool IsEnabled);

public sealed record ApplicationSecretAuditItemData(
    Guid Id,
    Guid ApplicationId,
    string ClientId,
    string? Description,
    string SecretPrefix,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedReason);

public sealed record ApplicationAuditMetadata(DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt, bool IsEnabled, bool IsDeleted);

public sealed record AdminAuditActor(Guid? UserId, string? UserEmail, string? IpAddress, string? UserAgent);

public sealed record ClientSecretData(
    Guid Id,
    string? Description,
    string SecretPrefix,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);

public interface IApplicationAuditService
{
    Task<IReadOnlyList<ApplicationAuditItemData>> ListAsync(bool includeDeleted);
}

public interface IApplicationSecretsAuditService
{
    Task<IReadOnlyList<ApplicationSecretAuditItemData>> ListAsync(bool activeOnly);
}

public interface IClientApplicationAdminService
{
    Task<bool> ActiveClientIdExistsAsync(string clientId, Guid? ignoreApplicationId = null);
    Task<ApplicationAuditMetadata> GetMetadataAsync(Guid applicationId);
    Task MarkCreatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor);
    Task MarkUpdatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor);
    Task SoftDeleteAsync(Guid applicationId, string clientId, AdminAuditActor actor);
    Task CreateSecretAsync(Guid applicationId, string description, string plainSecret, DateTimeOffset? expiresAtUtc);
    Task RevokeSecretAsync(Guid applicationId, Guid secretId, string reason);
    Task RevokeAllSecretsAsync(Guid applicationId, string reason);
    Task RevokeSecretsMissingFromFormAsync(Guid applicationId, HashSet<Guid> submittedSecretIds, string reason);
    Task<IReadOnlyList<ClientSecretData>> ListActiveSecretsAsync(Guid applicationId);
    Task<IReadOnlyList<ClientSecretData>> ListAllSecretsAsync(Guid applicationId);
}

public interface IApplicationAuditRepository
{
    Task<IReadOnlyList<ApplicationAuditItemData>> ListAsync(bool includeDeleted);
}

public interface IApplicationSecretsAuditRepository
{
    Task<IReadOnlyList<ApplicationSecretAuditItemData>> ListAsync(bool activeOnly);
}

public interface IClientApplicationRepository
{
    Task<bool> ActiveClientIdExistsAsync(string clientId, Guid? ignoreApplicationId = null);
    Task<ApplicationAuditMetadata> GetMetadataAsync(Guid applicationId);
    Task MarkCreatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor);
    Task MarkUpdatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor);
    Task SoftDeleteAsync(Guid applicationId, string clientId, AdminAuditActor actor);
}

public interface IApplicationSecretRepository
{
    Task CreateAsync(Guid applicationId, string description, string secretHash, string secretPrefix, DateTimeOffset? expiresAtUtc);
    Task RevokeAsync(Guid applicationId, Guid secretId, string reason);
    Task RevokeAllAsync(Guid applicationId, string reason);
    Task RevokeMissingFromFormAsync(Guid applicationId, HashSet<Guid> submittedSecretIds, string reason);
    Task<IReadOnlyList<ClientSecretData>> ListActiveAsync(Guid applicationId);
    Task<IReadOnlyList<ClientSecretData>> ListAllAsync(Guid applicationId);
}

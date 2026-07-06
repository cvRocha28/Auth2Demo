using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth2Demo.Infrastructure.Services.Admin;

public sealed class ApplicationAuditService : IApplicationAuditService
{
    private readonly IApplicationAuditRepository _repository;

    public ApplicationAuditService(IApplicationAuditRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ApplicationAuditItemData>> ListAsync(bool includeDeleted)
    {
        return _repository.ListAsync(includeDeleted);
    }
}

public sealed class ApplicationSecretsAuditService : IApplicationSecretsAuditService
{
    private readonly IApplicationSecretsAuditRepository _repository;

    public ApplicationSecretsAuditService(IApplicationSecretsAuditRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<ApplicationSecretAuditItemData>> ListAsync(bool activeOnly)
    {
        return _repository.ListAsync(activeOnly);
    }
}

public sealed class ClientApplicationAdminService : IClientApplicationAdminService
{
    private readonly IClientApplicationRepository _applications;
    private readonly IApplicationSecretRepository _secrets;

    public ClientApplicationAdminService(
        IClientApplicationRepository applications,
        IApplicationSecretRepository secrets)
    {
        _applications = applications;
        _secrets = secrets;
    }

    public Task<bool> ActiveClientIdExistsAsync(string clientId, Guid? ignoreApplicationId = null)
    {
        return _applications.ActiveClientIdExistsAsync(clientId, ignoreApplicationId);
    }

    public Task<ApplicationAuditMetadata> GetMetadataAsync(Guid applicationId)
    {
        return _applications.GetMetadataAsync(applicationId);
    }

    public Task MarkCreatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor)
    {
        return _applications.MarkCreatedAsync(applicationId, eventType, clientId, actor);
    }

    public Task MarkUpdatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor)
    {
        return _applications.MarkUpdatedAsync(applicationId, eventType, clientId, actor);
    }

    public Task SoftDeleteAsync(Guid applicationId, string clientId, AdminAuditActor actor)
    {
        return _applications.SoftDeleteAsync(applicationId, clientId, actor);
    }

    public async Task CreateSecretAsync(Guid applicationId, string description, string plainSecret, DateTimeOffset? expiresAtUtc)
    {
        var secret = new IdentityApplicationSecret
        {
            ApplicationId = applicationId,
            Description = description,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            SecretPrefix = GetSecretPrefix(plainSecret)
        };

        var secretHash = new PasswordHasher<IdentityApplicationSecret>()
            .HashPassword(secret, plainSecret);

        await _secrets.CreateAsync(
            applicationId,
            description,
            secretHash,
            secret.SecretPrefix,
            expiresAtUtc);
    }

    public Task RevokeSecretAsync(Guid applicationId, Guid secretId, string reason)
    {
        return _secrets.RevokeAsync(applicationId, secretId, reason);
    }

    public Task RevokeAllSecretsAsync(Guid applicationId, string reason)
    {
        return _secrets.RevokeAllAsync(applicationId, reason);
    }

    public Task RevokeSecretsMissingFromFormAsync(Guid applicationId, HashSet<Guid> submittedSecretIds, string reason)
    {
        return _secrets.RevokeMissingFromFormAsync(applicationId, submittedSecretIds, reason);
    }

    public Task<IReadOnlyList<ClientSecretData>> ListActiveSecretsAsync(Guid applicationId)
    {
        return _secrets.ListActiveAsync(applicationId);
    }

    public Task<IReadOnlyList<ClientSecretData>> ListAllSecretsAsync(Guid applicationId)
    {
        return _secrets.ListAllAsync(applicationId);
    }

    private static string GetSecretPrefix(string plainSecret)
    {
        var value = (plainSecret ?? string.Empty).Trim();
        return value.Length <= 3 ? value : value[..3];
    }
}

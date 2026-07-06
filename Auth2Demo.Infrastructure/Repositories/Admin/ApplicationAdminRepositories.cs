using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using System.Text.Json;

namespace Auth2Demo.Infrastructure.Repositories.Admin;

public sealed class ApplicationAuditRepository : IApplicationAuditRepository
{
    private readonly ApplicationDbContext _db;

    public ApplicationAuditRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApplicationAuditItemData>> ListAsync(bool includeDeleted)
    {
        var query = _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(application => !EF.Property<bool>(application, "IsDeleted"));
        }

        return await query
            .OrderByDescending(application => EF.Property<DateTimeOffset>(application, "CreatedAt"))
            .ThenBy(application => application.ClientId)
            .Select(application => new ApplicationAuditItemData(
                application.Id,
                application.ClientId ?? string.Empty,
                application.DisplayName,
                application.ClientType,
                application.ConsentType,
                EF.Property<DateTimeOffset>(application, "CreatedAt"),
                EF.Property<Guid?>(application, "CreatedByUserId"),
                EF.Property<DateTimeOffset?>(application, "UpdatedAt"),
                EF.Property<Guid?>(application, "UpdatedByUserId"),
                EF.Property<DateTimeOffset?>(application, "DeletedAt"),
                EF.Property<Guid?>(application, "DeletedByUserId"),
                EF.Property<bool>(application, "IsDeleted"),
                EF.Property<bool>(application, "IsEnabled")))
            .ToListAsync();
    }
}

public sealed class ApplicationSecretsAuditRepository : IApplicationSecretsAuditRepository
{
    private readonly ApplicationDbContext _db;

    public ApplicationSecretsAuditRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ApplicationSecretAuditItemData>> ListAsync(bool activeOnly)
    {
        var applications = _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .IgnoreQueryFilters()
            .AsNoTracking();

        var query = from secret in _db.IdentityApplicationSecrets.AsNoTracking()
                    join application in applications on secret.ApplicationId equals application.Id
                    select new
                    {
                        secret.Id,
                        secret.ApplicationId,
                        ClientId = application.ClientId ?? string.Empty,
                        secret.Description,
                        secret.SecretPrefix,
                        secret.CreatedAtUtc,
                        secret.ExpiresAtUtc,
                        secret.RevokedAtUtc,
                        secret.RevokedReason
                    };

        if (activeOnly)
        {
            var now = DateTimeOffset.UtcNow;
            query = query.Where(secret => secret.RevokedAtUtc == null && (secret.ExpiresAtUtc == null || secret.ExpiresAtUtc > now));
        }

        return await query
            .OrderByDescending(secret => secret.CreatedAtUtc)
            .ThenBy(secret => secret.ClientId)
            .Select(secret => new ApplicationSecretAuditItemData(
                secret.Id,
                secret.ApplicationId,
                secret.ClientId,
                secret.Description,
                secret.SecretPrefix,
                secret.CreatedAtUtc,
                secret.ExpiresAtUtc,
                secret.RevokedAtUtc,
                secret.RevokedReason))
            .ToListAsync();
    }
}

public sealed class ClientApplicationRepository : IClientApplicationRepository
{
    private readonly ApplicationDbContext _db;

    public ClientApplicationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ActiveClientIdExistsAsync(string clientId, Guid? ignoreApplicationId = null)
    {
        clientId = clientId.Trim();

        return await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .IgnoreQueryFilters()
            .AnyAsync(application =>
                application.ClientId == clientId &&
                !EF.Property<bool>(application, "IsDeleted") &&
                (!ignoreApplicationId.HasValue || application.Id != ignoreApplicationId.Value));
    }

    public async Task<ApplicationAuditMetadata> GetMetadataAsync(Guid applicationId)
    {
        var metadata = await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .IgnoreQueryFilters()
            .Where(application => application.Id == applicationId)
            .Select(application => new ApplicationAuditMetadata(
                EF.Property<DateTimeOffset>(application, "CreatedAt"),
                EF.Property<DateTimeOffset?>(application, "UpdatedAt"),
                EF.Property<bool>(application, "IsEnabled"),
                EF.Property<bool>(application, "IsDeleted")))
            .FirstOrDefaultAsync();

        return metadata ?? new ApplicationAuditMetadata(null, null, true, false);
    }

    public async Task MarkCreatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor)
    {
        var entity = await FindApplicationAsync(applicationId);
        if (entity is not null)
        {
            var entry = _db.Entry(entity);
            entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
            entry.Property("CreatedByUserId").CurrentValue = actor.UserId;
            entry.Property("IsEnabled").CurrentValue = true;
            entry.Property("IsDeleted").CurrentValue = false;
        }

        AddClientAudit(eventType, "Applications", "Success", clientId, applicationId, actor);
        await _db.SaveChangesAsync();
    }

    public async Task MarkUpdatedAsync(Guid applicationId, string eventType, string clientId, AdminAuditActor actor)
    {
        var entity = await FindApplicationAsync(applicationId);
        if (entity is not null)
        {
            var entry = _db.Entry(entity);
            entry.Property("UpdatedAt").CurrentValue = DateTimeOffset.UtcNow;
            entry.Property("UpdatedByUserId").CurrentValue = actor.UserId;
        }

        AddClientAudit(eventType, "Applications", "Success", clientId, applicationId, actor);
        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid applicationId, string clientId, AdminAuditActor actor)
    {
        var entity = await FindApplicationAsync(applicationId);
        if (entity is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        entity.ClientId = $"{clientId}__deleted__{now:yyyyMMddHHmmssfff}";

        var entry = _db.Entry(entity);
        entry.Property("UpdatedAt").CurrentValue = now;
        entry.Property("UpdatedByUserId").CurrentValue = actor.UserId;
        entry.Property("IsDeleted").CurrentValue = true;
        entry.Property("DeletedAt").CurrentValue = now;
        entry.Property("DeletedByUserId").CurrentValue = actor.UserId;
        entry.Property("IsEnabled").CurrentValue = false;

        AddClientAudit("ClientSoftDeleted", "Applications", "Success", clientId, applicationId, actor);
        await _db.SaveChangesAsync();
    }

    private Task<OpenIddictEntityFrameworkCoreApplication<Guid>?> FindApplicationAsync(Guid applicationId)
    {
        return _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(application => application.Id == applicationId);
    }

    private void AddClientAudit(string eventType, string category, string outcome, string clientId, Guid applicationId, AdminAuditActor actor)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Category = category,
            Outcome = outcome,
            UserId = actor.UserId,
            UserEmail = actor.UserEmail,
            IpAddress = actor.IpAddress,
            UserAgent = actor.UserAgent,
            Provider = "Admin",
            Description = $"Client '{clientId}' ({applicationId})",
            DataJson = JsonSerializer.Serialize(new
            {
                ClientId = clientId,
                ApplicationId = applicationId
            })
        });
    }
}

public sealed class ApplicationSecretRepository : IApplicationSecretRepository
{
    private readonly ApplicationDbContext _db;

    public ApplicationSecretRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task CreateAsync(Guid applicationId, string description, string secretHash, string secretPrefix, DateTimeOffset? expiresAtUtc)
    {
        _db.IdentityApplicationSecrets.Add(new IdentityApplicationSecret
        {
            ApplicationId = applicationId,
            Description = description,
            SecretHash = secretHash,
            SecretPrefix = secretPrefix,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        });

        await _db.SaveChangesAsync();
    }

    public async Task RevokeAsync(Guid applicationId, Guid secretId, string reason)
    {
        var secret = await _db.IdentityApplicationSecrets
            .FirstOrDefaultAsync(item => item.Id == secretId && item.ApplicationId == applicationId);

        if (secret is null || secret.RevokedAtUtc is not null)
        {
            return;
        }

        secret.RevokedAtUtc = DateTimeOffset.UtcNow;
        secret.RevokedReason = reason;
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllAsync(Guid applicationId, string reason)
    {
        var secrets = await _db.IdentityApplicationSecrets
            .Where(secret => secret.ApplicationId == applicationId && secret.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var secret in secrets)
        {
            secret.RevokedAtUtc = DateTimeOffset.UtcNow;
            secret.RevokedReason = reason;
        }

        if (secrets.Count > 0)
        {
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeMissingFromFormAsync(Guid applicationId, HashSet<Guid> submittedSecretIds, string reason)
    {
        var activeSecrets = await _db.IdentityApplicationSecrets
            .Where(secret => secret.ApplicationId == applicationId && secret.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var secret in activeSecrets.Where(secret => !submittedSecretIds.Contains(secret.Id)))
        {
            secret.RevokedAtUtc = DateTimeOffset.UtcNow;
            secret.RevokedReason = reason;
        }

        if (activeSecrets.Any(secret => secret.RevokedReason == reason))
        {
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<ClientSecretData>> ListActiveAsync(Guid applicationId)
    {
        return await _db.IdentityApplicationSecrets
            .AsNoTracking()
            .Where(secret => secret.ApplicationId == applicationId && secret.RevokedAtUtc == null)
            .OrderBy(secret => secret.CreatedAtUtc)
            .Select(secret => new ClientSecretData(
                secret.Id,
                secret.Description,
                secret.SecretPrefix,
                secret.CreatedAtUtc,
                secret.ExpiresAtUtc,
                secret.RevokedAtUtc))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ClientSecretData>> ListAllAsync(Guid applicationId)
    {
        return await _db.IdentityApplicationSecrets
            .AsNoTracking()
            .Where(secret => secret.ApplicationId == applicationId)
            .OrderByDescending(secret => secret.CreatedAtUtc)
            .Select(secret => new ClientSecretData(
                secret.Id,
                secret.Description,
                secret.SecretPrefix,
                secret.CreatedAtUtc,
                secret.ExpiresAtUtc,
                secret.RevokedAtUtc))
            .ToListAsync();
    }
}

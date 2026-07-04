using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth2Demo.Infrastructure.Repositories.Portal;

public sealed class ExternalProviderRepository : IExternalProviderRepository
{
    private readonly ApplicationDbContext _db;

    public ExternalProviderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ExternalProviderData>> GetEnabledForLoginAsync(ISet<string> configuredSchemes)
    {
        return await _db.IdentityProviders
            .AsNoTracking()
            .Where(x =>
                x.IsEnabled &&
                configuredSchemes.Contains(x.Scheme) &&
                x.ClientId != null &&
                x.ClientId != string.Empty &&
                x.ClientSecret != null &&
                x.ClientSecret != string.Empty)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new ExternalProviderData
            {
                DisplayName = x.DisplayName,
                Scheme = x.Scheme,
                ButtonText = x.ButtonText ?? $"Continuar com {x.DisplayName}"
            })
            .ToListAsync();
    }

    public Task<bool> IsProviderEnabledAsync(string provider)
    {
        return _db.IdentityProviders
            .AsNoTracking()
            .AnyAsync(x =>
                x.IsEnabled &&
                x.Scheme == provider &&
                x.ClientId != null &&
                x.ClientId != string.Empty &&
                x.ClientSecret != null &&
                x.ClientSecret != string.Empty);
    }
}

public sealed class PerfilRepository : IPerfilRepository
{
    private readonly ApplicationDbContext _db;

    public PerfilRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserSession>> GetSessionsAsync(Guid userId, int take)
    {
        return await _db.UserSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<UserDevice>> GetDevicesAsync(Guid userId, int take)
    {
        return await _db.UserDevices
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(Guid userId, int take)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MfaMethod>> GetMfaMethodsAsync(Guid userId)
    {
        return await _db.MfaMethods
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PasskeyCredential>> GetPasskeysAsync(Guid userId)
    {
        return await _db.PasskeyCredentials
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}

public sealed class AccountSecurityRepository : IAccountSecurityRepository
{
    private readonly ApplicationDbContext _db;

    public AccountSecurityRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task UpsertMfaMethodAsync(MfaMethod method)
    {
        var entry = await _db.MfaMethods.FirstOrDefaultAsync(x =>
            x.UserId == method.UserId &&
            x.Method == method.Method);

        if (entry is null)
        {
            _db.MfaMethods.Add(method);
        }
        else
        {
            entry.UserEmail = method.UserEmail;
            entry.DisplayName = method.DisplayName;
            entry.IsEnabled = method.IsEnabled;
            entry.IsDefault = method.IsDefault;
            entry.LastUsedAt = method.LastUsedAt ?? entry.LastUsedAt;
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateMfaMethodStatusAsync(Guid userId, string method, bool enabled)
    {
        var entry = await _db.MfaMethods.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Method == method);

        if (entry is null)
        {
            return;
        }

        entry.IsEnabled = enabled;
        await _db.SaveChangesAsync();
    }

    public async Task RecordAuditAsync(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }

    public async Task AddSessionAsync(UserSession session)
    {
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();
    }

    public async Task UpsertKnownDeviceAsync(UserDevice device, string? userAgent)
    {
        var knownDevice = await _db.UserDevices.FirstOrDefaultAsync(x =>
            x.UserId == device.UserId &&
            x.UserAgent == userAgent);

        if (knownDevice is null)
        {
            _db.UserDevices.Add(device);
        }
        else
        {
            knownDevice.IpAddress = device.IpAddress;
            knownDevice.LastSeenAt = device.LastSeenAt;
        }

        await _db.SaveChangesAsync();
    }
}

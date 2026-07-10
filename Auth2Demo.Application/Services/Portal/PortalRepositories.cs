using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Domain.Security;

namespace Auth2Demo.Application.Services.Portal;

public interface IExternalProviderRepository
{
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledForLoginAsync(ISet<string> configuredSchemes);
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledForApplicationAsync(string? clientId, ISet<string> configuredSchemes);
    Task<bool> IsProviderEnabledAsync(string provider);
    Task<bool> IsProviderEnabledForApplicationAsync(string provider, string? clientId);
}

public interface IPerfilRepository
{
    Task<IReadOnlyList<UserSession>> GetSessionsAsync(Guid userId, int take);
    Task<IReadOnlyList<UserDevice>> GetDevicesAsync(Guid userId, int take);
    Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(Guid userId, int take);
    Task<IReadOnlyList<MfaMethod>> GetMfaMethodsAsync(Guid userId);
    Task<IReadOnlyList<PasskeyCredential>> GetPasskeysAsync(Guid userId);
}

public interface IAccountSecurityRepository
{
    Task UpsertMfaMethodAsync(MfaMethod method);
    Task UpdateMfaMethodStatusAsync(Guid userId, string method, bool enabled);
    Task RecordAuditAsync(AuditLog auditLog);
    Task AddSessionAsync(UserSession session);
    Task UpsertKnownDeviceAsync(UserDevice device, string? userAgent);
}

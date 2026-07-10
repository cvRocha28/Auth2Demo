using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth2Demo.Infrastructure.Services.Portal;

public sealed class PerfilIndexData
{
    public required ApplicationUser User { get; init; }
    public required string DisplayName { get; set; }
    public required string CurrentLocale { get; init; }
    public required bool IsAdmin { get; init; }
    public required IReadOnlyList<UserLoginInfo> ExternalLogins { get; init; }
    public required IReadOnlyList<UserSession> Sessions { get; init; }
    public required IReadOnlyList<UserDevice> Devices { get; init; }
    public required IReadOnlyList<AuditLog> AuditLogs { get; init; }
    public required IReadOnlyList<MfaMethod> MfaMethods { get; init; }
    public required IReadOnlyList<PasskeyCredential> Passkeys { get; init; }
}


public sealed class ExternalProviderService : IExternalProviderService
{
    private readonly IExternalProviderRepository _providers;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ExternalProviderService(
        IExternalProviderRepository providers,
        SignInManager<ApplicationUser> signInManager)
    {
        _providers = providers;
        _signInManager = signInManager;
    }

    public async Task<IReadOnlyList<ExternalProviderData>> GetEnabledForLoginAsync()
    {
        var configuredSchemes = await GetConfiguredSchemesAsync();
        return await _providers.GetEnabledForLoginAsync(configuredSchemes);
    }

    public async Task<IReadOnlyList<ExternalProviderData>> GetEnabledForApplicationAsync(string? clientId)
    {
        var configuredSchemes = await GetConfiguredSchemesAsync();
        return await _providers.GetEnabledForApplicationAsync(clientId, configuredSchemes);
    }

    private async Task<ISet<string>> GetConfiguredSchemesAsync()
    {
        return (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public interface IPerfilService
{
    Task<PerfilIndexData> BuildIndexAsync(ApplicationUser user);
    Task<bool> UpdateDisplayNameAsync(ApplicationUser user, string displayName);
    Task UpdateLocaleAsync(ApplicationUser user, string locale);
}

public sealed class PerfilService : IPerfilService
{
    private readonly IPerfilRepository _perfil;
    private readonly UserManager<ApplicationUser> _userManager;

    public PerfilService(
        IPerfilRepository perfil,
        UserManager<ApplicationUser> userManager)
    {
        _perfil = perfil;
        _userManager = userManager;
    }

    public async Task<PerfilIndexData> BuildIndexAsync(ApplicationUser user)
    {
        var externalLogins = await _userManager.GetLoginsAsync(user);

        return new PerfilIndexData
        {
            User = user,
            DisplayName = user.DisplayName,
            CurrentLocale = string.IsNullOrWhiteSpace(user.Locale) ? "en-US" : user.Locale,
            IsAdmin = await _userManager.IsInRoleAsync(user, "admin"),
            ExternalLogins = (IReadOnlyList<UserLoginInfo>)externalLogins,
            Sessions = await _perfil.GetSessionsAsync(user.Id, 10),
            Devices = await _perfil.GetDevicesAsync(user.Id, 10),
            AuditLogs = await _perfil.GetAuditLogsAsync(user.Id, 10),
            MfaMethods = await _perfil.GetMfaMethodsAsync(user.Id),
            Passkeys = await _perfil.GetPasskeysAsync(user.Id)
        };
    }

    public async Task<bool> UpdateDisplayNameAsync(ApplicationUser user, string displayName)
    {
        user.DisplayName = displayName.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task UpdateLocaleAsync(ApplicationUser user, string locale)
    {
        user.Locale = locale;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _userManager.UpdateAsync(user);
    }
}

public interface IAccountSecurityService
{
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledExternalProvidersAsync();
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledExternalProvidersForApplicationAsync(string? clientId);
    Task<bool> IsProviderEnabledAsync(string provider);
    Task<bool> IsProviderEnabledForApplicationAsync(string provider, string? clientId);
    Task UpsertMfaMethodAsync(ApplicationUser user, string method, string displayName, bool enabled, bool isDefault, DateTimeOffset? lastUsedAt);
    Task UpdateMfaMethodStatusAsync(Guid userId, string method, bool enabled);
    Task RecordLoginAsync(ApplicationUser user, string provider, string outcome, string description, string? ipAddress, string? userAgent);
    Task RecordAuditAsync(string eventType, string category, string outcome, Guid? userId, string? userEmail, string description, string? provider, string? ipAddress, string? userAgent);
}

public sealed class AccountSecurityService : IAccountSecurityService
{
    private readonly IAccountSecurityRepository _accountSecurity;
    private readonly IExternalProviderService _externalProviders;
    private readonly IExternalProviderRepository _providerRepository;

    public AccountSecurityService(
        IAccountSecurityRepository accountSecurity,
        IExternalProviderService externalProviders,
        IExternalProviderRepository providerRepository)
    {
        _accountSecurity = accountSecurity;
        _externalProviders = externalProviders;
        _providerRepository = providerRepository;
    }

    public Task<IReadOnlyList<ExternalProviderData>> GetEnabledExternalProvidersAsync()
    {
        return _externalProviders.GetEnabledForLoginAsync();
    }

    public Task<IReadOnlyList<ExternalProviderData>> GetEnabledExternalProvidersForApplicationAsync(string? clientId)
    {
        return _externalProviders.GetEnabledForApplicationAsync(clientId);
    }

    public Task<bool> IsProviderEnabledAsync(string provider)
    {
        return _providerRepository.IsProviderEnabledAsync(provider);
    }

    public Task<bool> IsProviderEnabledForApplicationAsync(string provider, string? clientId)
    {
        return _providerRepository.IsProviderEnabledForApplicationAsync(provider, clientId);
    }

    public Task UpsertMfaMethodAsync(
        ApplicationUser user,
        string method,
        string displayName,
        bool enabled,
        bool isDefault,
        DateTimeOffset? lastUsedAt)
    {
        return _accountSecurity.UpsertMfaMethodAsync(new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            UserEmail = user.Email ?? user.UserName ?? string.Empty,
            Method = method,
            DisplayName = displayName,
            IsEnabled = enabled,
            IsDefault = isDefault,
            LastUsedAt = lastUsedAt
        });
    }

    public Task UpdateMfaMethodStatusAsync(Guid userId, string method, bool enabled)
    {
        return _accountSecurity.UpdateMfaMethodStatusAsync(userId, method, enabled);
    }

    public async Task RecordLoginAsync(
        ApplicationUser user,
        string provider,
        string outcome,
        string description,
        string? ipAddress,
        string? userAgent)
    {
        await RecordAuditAsync(
            $"{provider} Login",
            "Authentication",
            outcome,
            user.Id,
            user.Email,
            description,
            provider,
            ipAddress,
            userAgent);

        await _accountSecurity.AddSessionAsync(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            UserEmail = user.Email ?? user.UserName ?? string.Empty,
            SessionId = Guid.NewGuid().ToString("N"),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceName = ParseDeviceName(userAgent),
            LastSeenAt = DateTimeOffset.UtcNow
        });

        await _accountSecurity.UpsertKnownDeviceAsync(new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            UserEmail = user.Email ?? user.UserName ?? string.Empty,
            Name = ParseDeviceName(userAgent),
            Browser = ParseBrowser(userAgent),
            OperatingSystem = ParseOperatingSystem(userAgent),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = userAgent ?? Guid.NewGuid().ToString("N"),
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        }, userAgent);
    }

    public Task RecordAuditAsync(
        string eventType,
        string category,
        string outcome,
        Guid? userId,
        string? userEmail,
        string description,
        string? provider,
        string? ipAddress,
        string? userAgent)
    {
        return _accountSecurity.RecordAuditAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Category = category,
            Outcome = outcome,
            UserId = userId,
            UserEmail = userEmail,
            Provider = provider,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = description
        });
    }

    private static string ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Dispositivo desconhecido";
        }

        return $"{ParseBrowser(userAgent)} em {ParseOperatingSystem(userAgent)}";
    }

    private static string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Desconhecido";
        }

        if (userAgent.Contains("Edg/"))
        {
            return "Microsoft Edge";
        }

        if (userAgent.Contains("Chrome/"))
        {
            return "Chrome";
        }

        if (userAgent.Contains("Firefox/"))
        {
            return "Firefox";
        }

        if (userAgent.Contains("Safari/"))
        {
            return "Safari";
        }

        return "Navegador";
    }

    private static string ParseOperatingSystem(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "Desconhecido";
        }

        if (userAgent.Contains("Windows"))
        {
            return "Windows";
        }

        if (userAgent.Contains("Android"))
        {
            return "Android";
        }

        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
        {
            return "iOS";
        }

        if (userAgent.Contains("Mac OS"))
        {
            return "macOS";
        }

        if (userAgent.Contains("Linux"))
        {
            return "Linux";
        }

        return "Sistema operacional";
    }
}

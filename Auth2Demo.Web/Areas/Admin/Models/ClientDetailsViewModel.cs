namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientDetailsViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string ConsentType { get; set; } = string.Empty;
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IReadOnlyList<string> RedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedScopes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> GrantTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Endpoints { get; set; } = Array.Empty<string>();
    public bool RequirePkce { get; set; }
    public IReadOnlyList<ClientScopeOptionViewModel> AvailableScopes { get; set; } = Array.Empty<ClientScopeOptionViewModel>();
    public IReadOnlyList<ClientSecretViewModel> Secrets { get; set; } = Array.Empty<ClientSecretViewModel>();
    public IReadOnlyList<ClientRequiredClaimInputViewModel> RequiredClaims { get; set; } = Array.Empty<ClientRequiredClaimInputViewModel>();
    public ClientBrandingViewModel? Branding { get; set; }

    public int ActiveSecretCount => Secrets.Count(secret => secret.IsActive);
    public int ExpiredSecretCount => Secrets.Count(secret => secret.IsExpired);
    public int RevokedSecretCount => Secrets.Count(secret => secret.IsRevoked);
}

public sealed class ClientSecretViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SecretPrefix { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired => ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;
    public string ExpirationLabel => ExpiresAtUtc.HasValue ? ExpiresAtUtc.Value.ToString("yyyy-MM-dd HH:mm") : "Nunca expira";
    public string StatusLabel => IsRevoked ? "Revogada" : IsExpired ? "Expirada" : "Ativa";
    public string MaskedValue => string.IsNullOrWhiteSpace(SecretPrefix) ? "***************" : $"{SecretPrefix}***************";
    public string SecretIdDisplay => Id.ToString("D");
}


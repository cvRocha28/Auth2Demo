using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

public sealed class IdentityProvider : AuditableEntity<Guid>
{
    private IdentityProvider() { }

    public IdentityProvider(string name, string displayName, string scheme, IdentityProviderKind kind)
    {
        Id = Guid.NewGuid();
        Name = name;
        DisplayName = displayName;
        Scheme = scheme;
        Kind = kind;
        IsEnabled = false;
        SortOrder = 100;
    }

    public Guid? CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Scheme { get; private set; } = string.Empty;
    public IdentityProviderKind Kind { get; private set; }
    public string? IconCssClass { get; private set; }
    public string? ButtonText { get; private set; }
    public string? ClientId { get; private set; }
    public string? ClientSecret { get; private set; }
    public string? Authority { get; private set; }
    public string? CallbackPath { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsSystemProvider { get; private set; }
    public int SortOrder { get; private set; }

    public void Update(
        string displayName,
        string scheme,
        IdentityProviderKind kind,
        string? iconCssClass,
        string? buttonText,
        string? clientId,
        string? clientSecret,
        string? authority,
        string? callbackPath,
        bool isEnabled,
        int sortOrder,
        Guid? companyId = null)
    {
        DisplayName = displayName.Trim();
        Scheme = scheme.Trim();
        Kind = kind;
        IconCssClass = string.IsNullOrWhiteSpace(iconCssClass) ? null : iconCssClass.Trim();
        ButtonText = string.IsNullOrWhiteSpace(buttonText) ? null : buttonText.Trim();
        ClientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId.Trim();
        ClientSecret = string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret.Trim();
        Authority = string.IsNullOrWhiteSpace(authority) ? null : authority.Trim();
        CallbackPath = string.IsNullOrWhiteSpace(callbackPath) ? null : callbackPath.Trim();
        IsEnabled = isEnabled;
        SortOrder = sortOrder;
        CompanyId = companyId;
        MarkAsUpdated();
    }

    public void AssignToCompany(Guid? companyId)
    {
        CompanyId = companyId;
        MarkAsUpdated();
    }

    public void MarkAsSystemProvider()
    {
        IsSystemProvider = true;
    }
}

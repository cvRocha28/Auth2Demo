using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

public sealed class Company : AuditableEntity<Guid>
{
    private Company() { }

    public Company(string name, string displayName)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        DisplayName = displayName.Trim();
        IsEnabled = true;
        IsDefault = false;
    }

    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DomainHint { get; private set; }
    public string? Country { get; private set; }
    public string? Culture { get; private set; }
    public string? TimeZone { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsDefault { get; private set; }

    public void Update(string displayName, string? description, string? domainHint, string? country, string? culture, string? timeZone, bool isEnabled, bool isDefault)
    {
        DisplayName = displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DomainHint = string.IsNullOrWhiteSpace(domainHint) ? null : domainHint.Trim().ToLowerInvariant();
        Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant();
        Culture = string.IsNullOrWhiteSpace(culture) ? null : culture.Trim();
        TimeZone = string.IsNullOrWhiteSpace(timeZone) ? null : timeZone.Trim();
        IsEnabled = isEnabled;
        IsDefault = isDefault;
        MarkAsUpdated();
    }
}

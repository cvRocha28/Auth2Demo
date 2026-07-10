using Auth2Demo.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class IdentityProviderListItemViewModel
{
    public Guid Id { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Scheme { get; init; } = string.Empty;
    public IdentityProviderKind Kind { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsSystemProvider { get; init; }
    public int SortOrder { get; init; }
    public bool HasClientId { get; init; }
    public bool HasClientSecret { get; init; }
}

public sealed class IdentityProviderEditViewModel
{
    public Guid? Id { get; set; }

    public Guid? CompanyId { get; set; }
    public IReadOnlyList<CompanyListItemViewModel> Companies { get; set; } = Array.Empty<CompanyListItemViewModel>();

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Scheme { get; set; } = string.Empty;

    public IdentityProviderKind Kind { get; set; } = IdentityProviderKind.Google;

    [StringLength(120)]
    public string? IconCssClass { get; set; }

    [StringLength(160)]
    public string? ButtonText { get; set; }

    [StringLength(400)]
    public string? ClientId { get; set; }

    [StringLength(1000)]
    public string? ClientSecret { get; set; }

    public bool HasClientSecret { get; set; }

    [StringLength(500)]
    public string? Authority { get; set; }

    [StringLength(200)]
    public string? CallbackPath { get; set; }

    public bool IsEnabled { get; set; }
    public bool IsSystemProvider { get; set; }
    public int SortOrder { get; set; } = 100;
}

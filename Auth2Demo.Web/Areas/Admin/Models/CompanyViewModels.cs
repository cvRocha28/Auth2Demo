using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class CompanyListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? DomainHint { get; init; }
    public string? Country { get; init; }
    public string? Culture { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsDefault { get; init; }
    public int ProviderCount { get; init; }
}

public sealed class CompanyEditViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(160)]
    public string? DomainHint { get; set; }

    [StringLength(8)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? Culture { get; set; }

    [StringLength(120)]
    public string? TimeZone { get; set; }

    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ScopeCreateViewModel
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required] public string Resources { get; set; } = "resource_server";
}


public sealed class ScopeListItemViewModel
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

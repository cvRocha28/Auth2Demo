using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class UserCreateViewModel
{
    [Required, MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(10)] public string Password { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = true;
    public string[] Roles { get; set; } = [];
    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

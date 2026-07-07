using System.ComponentModel.DataAnnotations;
using Auth2Demo.Domain.Identity;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class UserEditViewModel
{
    public Guid Id { get; set; }
    [Required, MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(256)] public string UserName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool EmailConfirmed { get; set; }
    public string[] Roles { get; set; } = [];
    public IReadOnlyList<string> AvailableRoles { get; set; } = Array.Empty<string>();
}

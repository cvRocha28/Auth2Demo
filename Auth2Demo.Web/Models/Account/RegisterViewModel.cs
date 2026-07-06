using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class RegisterViewModel
{
    [Required, MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class ResetPasswordViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

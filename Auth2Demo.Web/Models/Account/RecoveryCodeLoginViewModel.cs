using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class RecoveryCodeLoginViewModel
{
    [Required(ErrorMessage = "RecoveryCodeRequired")]
    [Display(Name = "RecoveryCode")]
    public string RecoveryCode { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class ForgotPasswordViewModel
{
    [Required, MaxLength(256)]
    [Display(Name = "UsernameOrEmail")]
    public string Login { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

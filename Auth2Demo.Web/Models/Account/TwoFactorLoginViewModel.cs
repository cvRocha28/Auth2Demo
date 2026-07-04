using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class TwoFactorLoginViewModel
{
    [Required(ErrorMessage = "AuthenticationCodeRequired")]
    [Display(Name = "AuthenticationCode")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    [Display(Name = "Confiar neste dispositivo")]
    public bool RememberMachine { get; set; }

    public string? ReturnUrl { get; set; }
}

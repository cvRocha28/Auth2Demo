using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class LoginViewModel
{
    [Required, MaxLength(256)]
    [Display(Name = "UsernameOrEmail")]
    public string Login { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    public bool ShowResendEmailConfirmation { get; set; }
    public bool EnableLocalLogin { get; set; } = true;
    public IReadOnlyList<ExternalProviderViewModel> ExternalProviders { get; set; } = Array.Empty<ExternalProviderViewModel>();
}

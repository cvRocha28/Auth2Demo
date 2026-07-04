using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Models.Account;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    public bool ShowResendEmailConfirmation { get; set; }
    public IReadOnlyList<ExternalProviderViewModel> ExternalProviders { get; set; } = Array.Empty<ExternalProviderViewModel>();
}

namespace Auth2Demo.Web.Models.Account;

public sealed class PasswordResetSentViewModel
{
    public string Email { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
    public bool EmailDeliveryUnavailable { get; set; }
    public string? ReturnUrl { get; set; }
}

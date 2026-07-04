namespace Auth2Demo.Web.Models.Account;

public sealed class EmailConfirmationSentViewModel
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationLink { get; set; } = string.Empty;
}

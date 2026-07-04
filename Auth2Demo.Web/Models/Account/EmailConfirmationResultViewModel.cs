namespace Auth2Demo.Web.Models.Account;

public sealed class EmailConfirmationResultViewModel
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
}

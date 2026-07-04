namespace Auth2Demo.Web.Models.Account;

public sealed class RecoveryCodesViewModel
{
    public IReadOnlyList<string> Codes { get; set; } = Array.Empty<string>();
}

namespace Auth2Demo.Web.Models.Account;

public sealed class ExternalProviderViewModel
{
    public string DisplayName { get; init; } = string.Empty;
    public string Scheme { get; init; } = string.Empty;
    public string ButtonText { get; init; } = string.Empty;
}

namespace Auth2Demo.Application.Services.Portal;

public sealed class ExternalProviderData
{
    public string DisplayName { get; init; } = string.Empty;
    public string Scheme { get; init; } = string.Empty;
    public string ButtonText { get; init; } = string.Empty;
}

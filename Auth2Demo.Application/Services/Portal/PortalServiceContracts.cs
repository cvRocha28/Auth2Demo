namespace Auth2Demo.Application.Services.Portal;

public interface IExternalProviderService
{
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledForLoginAsync();
    Task<IReadOnlyList<ExternalProviderData>> GetEnabledForApplicationAsync(string? clientId);
}

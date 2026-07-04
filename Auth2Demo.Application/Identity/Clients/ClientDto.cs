namespace Auth2Demo.Application.Identity.Clients;

public sealed record ClientDto(
    string Id,
    string ClientId,
    string? DisplayName,
    string Type,
    string ConsentType,
    bool HasClientSecret);

namespace Auth2Demo.Web.Models.Authorization;

public sealed class AuthorizationAccessDeniedViewModel
{
    public required string ApplicationName { get; init; }
    public required string ClientId { get; init; }
    public required string Reason { get; init; }
    public required string CorrelationId { get; init; }
    public required string ReturnUrl { get; init; }
    public bool IsAssignmentRequired { get; init; }
}

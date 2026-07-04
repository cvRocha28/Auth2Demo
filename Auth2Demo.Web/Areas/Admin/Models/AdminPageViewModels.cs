using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class AuditLogsIndexViewModel
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AuditLog> Logs { get; init; } = Array.Empty<AuditLog>();
}

public sealed class UsersIndexViewModel
{
    public string? Query { get; init; }
    public IReadOnlyList<ApplicationUser> Users { get; init; } = Array.Empty<ApplicationUser>();
}

public sealed class TokenExplorerViewModel
{
    public string? Token { get; init; }
    public string? Payload { get; init; }
}

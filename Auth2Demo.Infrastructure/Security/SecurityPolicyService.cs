using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth2Demo.Infrastructure.Security;

public sealed class SecurityPolicyService : ISecurityPolicyService
{
    private readonly ApplicationDbContext _db;

    public SecurityPolicyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PasswordPolicyData> GetPasswordPolicyAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SecuritySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? new SecuritySettings();

        return new PasswordPolicyData(
            Math.Clamp(settings.PasswordRequiredLength, 4, 128),
            settings.RequireDigit,
            settings.RequireUppercase,
            settings.RequireLowercase,
            settings.RequireNonAlphanumeric);
    }
}

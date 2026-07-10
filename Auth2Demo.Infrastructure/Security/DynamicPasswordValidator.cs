using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auth2Demo.Infrastructure.Security;

public sealed class DynamicPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private readonly ApplicationDbContext _db;

    public DynamicPasswordValidator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        var settings = await _db.SecuritySettings.AsNoTracking().FirstOrDefaultAsync() ?? new SecuritySettings();
        var errors = new List<IdentityError>();
        var value = password ?? string.Empty;

        if (value.Length < settings.PasswordRequiredLength)
        {
            errors.Add(new IdentityError
            {
                Code = nameof(settings.PasswordRequiredLength),
                Description = $"Passwords must be at least {settings.PasswordRequiredLength} characters."
            });
        }

        if (settings.RequireDigit && !value.Any(char.IsDigit))
        {
            errors.Add(new IdentityError { Code = nameof(settings.RequireDigit), Description = "Passwords must contain at least one number." });
        }

        if (settings.RequireUppercase && !value.Any(char.IsUpper))
        {
            errors.Add(new IdentityError { Code = nameof(settings.RequireUppercase), Description = "Passwords must contain at least one uppercase letter." });
        }

        if (settings.RequireLowercase && !value.Any(char.IsLower))
        {
            errors.Add(new IdentityError { Code = nameof(settings.RequireLowercase), Description = "Passwords must contain at least one lowercase letter." });
        }

        if (settings.RequireNonAlphanumeric && !value.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            errors.Add(new IdentityError { Code = nameof(settings.RequireNonAlphanumeric), Description = "Passwords must contain at least one special character." });
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
    }
}

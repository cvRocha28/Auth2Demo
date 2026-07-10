using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Localization;
using Auth2Demo.Infrastructure.Services.Portal;
using Auth2Demo.Web.Models.Perfil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Controllers;

[Authorize]
public sealed class PerfilController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPerfilService _perfil;
    private readonly IAccountSecurityService _security;
    private readonly ISecurityPolicyService _securityPolicy;

    public PerfilController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPerfilService perfil,
        IAccountSecurityService security,
        ISecurityPolicyService securityPolicy)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _perfil = perfil;
        _security = security;
        _securityPolicy = securityPolicy;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var data = await _perfil.BuildIndexAsync(user);
        var passwordPolicy = await _securityPolicy.GetPasswordPolicyAsync(HttpContext.RequestAborted);

        return View(new PerfilIndexViewModel
        {
            User = data.User,
            DisplayName = data.DisplayName,
            CurrentLocale = data.CurrentLocale,
            IsAdmin = data.IsAdmin,
            ExternalLogins = data.ExternalLogins,
            Sessions = data.Sessions,
            Devices = data.Devices,
            AuditLogs = data.AuditLogs,
            MfaMethods = data.MfaMethods,
            Passkeys = data.Passkeys,
            HasLocalPassword = await _userManager.HasPasswordAsync(user),
            PasswordPolicy = new PasswordPolicyViewModel(
                passwordPolicy.RequiredLength,
                passwordPolicy.RequireDigit,
                passwordPolicy.RequireUppercase,
                passwordPolicy.RequireLowercase,
                passwordPolicy.RequireNonAlphanumeric)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarDisplayName(string displayName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        displayName = (displayName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            TempData["Error"] = "DisplayNameRequired";
            return RedirectToAction(nameof(Index));
        }

        if (displayName.Length > 100)
        {
            TempData["Error"] = "DisplayNameMaxLength";
            return RedirectToAction(nameof(Index));
        }

        if (!await _perfil.UpdateDisplayNameAsync(user, displayName))
        {
            TempData["Error"] = "ProfileUpdateFailed";
            return RedirectToAction(nameof(Index));
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "ProfileUpdated";

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(string? currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        newPassword = newPassword?.Trim() ?? string.Empty;
        confirmPassword = confirmPassword?.Trim() ?? string.Empty;
        currentPassword ??= string.Empty;

        var hasLocalPassword = await _userManager.HasPasswordAsync(user);

        if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["Error"] = "PasswordFieldsRequired";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            TempData["Error"] = "PasswordConfirmationDoesNotMatch";
            return RedirectToAction(nameof(Index));
        }

        if (hasLocalPassword && string.IsNullOrWhiteSpace(currentPassword))
        {
            TempData["Error"] = "CurrentPasswordRequired";
            return RedirectToAction(nameof(Index));
        }

        IdentityResult result = hasLocalPassword
            ? await _userManager.ChangePasswordAsync(user, currentPassword, newPassword)
            : await _userManager.AddPasswordAsync(user, newPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = "PasswordUpdateFailed";
            TempData["ErrorDetails"] = string.Join(" ", result.Errors.Select(x => x.Description));
            await RecordSecurityAuditAsync(user, hasLocalPassword ? "Local password change" : "Local password creation", "Failed");
            return RedirectToAction(nameof(Index));
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        await RecordSecurityAuditAsync(user, hasLocalPassword ? "Local password changed" : "Local password created", "Success");

        TempData["Success"] = hasLocalPassword ? "PasswordChangedSuccessfully" : "LocalPasswordCreatedSuccessfully";
        return RedirectToAction(nameof(Index));
    }

    private Task RecordSecurityAuditAsync(ApplicationUser user, string eventType, string outcome)
    {
        return _security.RecordAuditAsync(
            eventType,
            "AccountSecurity",
            outcome,
            user.Id,
            user.Email,
            eventType,
            "Password",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarIdioma(string locale, string? returnUrl = null)
    {
        locale = UserProfileRequestCultureProvider.NormalizeLocale(locale);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await _perfil.UpdateLocaleAsync(user, locale);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(locale)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        TempData["Success"] = "SavedLanguage";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}

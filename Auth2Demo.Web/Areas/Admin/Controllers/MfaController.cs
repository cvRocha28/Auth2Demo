using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QRCoder;
using System.Text.Encodings.Web;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class MfaController : Controller
{
    private static readonly string AuthenticatorProvider = TokenOptions.DefaultAuthenticatorProvider;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminMfaService _mfa;
    private readonly UrlEncoder _urlEncoder;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MfaController(
        UserManager<ApplicationUser> userManager,
        IAdminMfaService mfa,
        UrlEncoder urlEncoder,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _mfa = mfa;
        _urlEncoder = urlEncoder;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var data = await _mfa.GetIndexAsync();
        return View(new MfaAdminIndexViewModel
        {
            Users = data.Users.Select(x => new MfaUserRowViewModel
            {
                UserId = x.UserId,
                Email = x.Email,
                DisplayName = x.DisplayName,
                Status = x.Status,
                TwoFactorEnabled = x.TwoFactorEnabled,
                RecoveryCodesLeft = x.RecoveryCodesLeft,
                LastLoginAt = x.LastLoginAt,
                LastMfaUsedAt = x.LastMfaUsedAt,
                Methods = x.Methods
            }).ToArray(),
            TotalUsers = data.TotalUsers,
            EnabledUsers = data.EnabledUsers,
            WithoutMfaUsers = data.WithoutMfaUsers
        });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var data = await _mfa.GetDetailsAsync(id);
        if (data is null) return NotFound();

        return View(new MfaAdminDetailsViewModel
        {
            UserId = data.UserId,
            Email = data.Email,
            DisplayName = data.DisplayName,
            Status = data.Status,
            TwoFactorEnabled = data.TwoFactorEnabled,
            RecoveryCodesLeft = data.RecoveryCodesLeft,
            HasAuthenticator = data.HasAuthenticator,
            Methods = data.Methods
        });
    }

    [HttpGet]
    public async Task<IActionResult> SetupAuthenticator(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await EnsureAuthenticatorKeyAsync(user);
        return View(await BuildSetupViewModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAuthenticator(MfaAdminAuthenticatorSetupViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null) return NotFound();

        await EnsureAuthenticatorKeyAsync(user);

        var code = (model.Code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, AuthenticatorProvider, code);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.Code), _localizer["InvalidAuthenticatorCodeCheckDeviceTime"].Value);
            return View(await BuildSetupViewModelAsync(user, model.Code));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _mfa.UpsertMethodAsync(user, "Authenticator", _localizer["AuthenticatorApp"].Value, true, true, DateTimeOffset.UtcNow);

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _mfa.UpsertMethodAsync(user, "RecoveryCodes", _localizer["RecoveryCodes"].Value, true, false, null);
        await AuditAsync(user, "Admin Enable MFA", _localizer["AuditAdminEnabledMfaAuthenticator"].Value);

        TempData["Success"] = _localizer["MfaEnabledForUser"].Value;
        TempData["RecoveryCodes"] = string.Join("|", codes ?? Array.Empty<string>());
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _mfa.SetMethodEnabledAsync(user.Id, "Authenticator", false);
        await AuditAsync(user, "Admin Disable MFA", _localizer["AuditAdminDisabledUserMfa"].Value);
        TempData["Success"] = _localizer["MfaDisabledForUser"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticator(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _mfa.SetMethodEnabledAsync(user.Id, "Authenticator", false);
        await AuditAsync(user, "Admin Reset Authenticator", _localizer["AuditAdminResetUserAuthenticatorKey"].Value);
        TempData["Success"] = _localizer["AuthenticatorResetConfigureNewQrCode"].Value;
        return RedirectToAction(nameof(SetupAuthenticator), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMethod(Guid id, Guid methodId)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var deleteResult = await _mfa.DeleteMethodAsync(id, methodId);
        if (!deleteResult.Found) return NotFound();

        if (deleteResult.MethodName.Equals("Authenticator", StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
        }

        await AuditAsync(user, "Admin Delete MFA Method", string.Format(_localizer["AuditAdminRemovedMfaMethodFormat"].Value, deleteResult.MethodName));
        TempData["Success"] = _localizer["MfaMethodRemoved"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            TempData["Error"] = _localizer["UserMustHaveMfaEnabledToGenerateCodes"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _mfa.UpsertMethodAsync(user, "RecoveryCodes", _localizer["RecoveryCodes"].Value, true, false, null);
        await AuditAsync(user, "Admin Generate Recovery Codes", _localizer["AuditAdminGeneratedUserRecoveryCodes"].Value);
        TempData["RecoveryCodes"] = string.Join("|", codes ?? Array.Empty<string>());
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<MfaAdminAuthenticatorSetupViewModel> BuildSetupViewModelAsync(ApplicationUser user, string? code = null)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? user.Id.ToString();
        var uri = GenerateQrCodeUri(email, key);

        return new MfaAdminAuthenticatorSetupViewModel
        {
            UserId = user.Id,
            Email = email,
            DisplayName = user.DisplayName,
            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            SharedKey = FormatKey(key),
            AuthenticatorUri = uri,
            QrCodeDataUrl = GenerateQrCodeDataUrl(uri),
            Code = code ?? string.Empty
        };
    }

    private async Task EnsureAuthenticatorKeyAsync(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key)) await _userManager.ResetAuthenticatorKeyAsync(user);
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        const string issuer = "Auth2Demo";
        return string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            _urlEncoder.Encode(issuer),
            _urlEncoder.Encode(email),
            unformattedKey);
    }

    private static string GenerateQrCodeDataUrl(string uri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(12);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private static string FormatKey(string key)
    {
        var result = new List<string>();
        var current = 0;
        while (current + 4 < key.Length)
        {
            result.Add(key.Substring(current, 4).ToLowerInvariant());
            current += 4;
        }
        if (current < key.Length) result.Add(key[current..].ToLowerInvariant());
        return string.Join(" ", result);
    }

    private Task AuditAsync(ApplicationUser user, string eventType, string description)
    {
        return _mfa.AuditAsync(
            user,
            eventType,
            description,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }

}

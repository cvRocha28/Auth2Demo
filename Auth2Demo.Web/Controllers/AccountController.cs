using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Models.Account;
using Auth2Demo.Infrastructure.Services.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using QRCoder;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Auth2Demo.Web;
using Auth2Demo.Web.Services.Branding;

namespace Auth2Demo.Web.Controllers;

public sealed class AccountController : Controller
{
    private static readonly string AuthenticatorProvider = TokenOptions.DefaultAuthenticatorProvider;

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountSecurityService _security;
    private readonly ILocalAccountService _localAccounts;
    private readonly UrlEncoder _urlEncoder;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IBrandingResolver _brandingResolver;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAccountSecurityService security,
        ILocalAccountService localAccounts,
        UrlEncoder urlEncoder,
        IStringLocalizer<SharedResource> localizer,
        IBrandingResolver brandingResolver)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _security = security;
        _localAccounts = localAccounts;
        _urlEncoder = urlEncoder;
        _localizer = localizer;
        _brandingResolver = brandingResolver;
    }

    private async Task<IReadOnlyList<ExternalProviderViewModel>> GetExternalProvidersAsync(string? returnUrl)
    {
        var branding = await _brandingResolver.ResolveAsync(HttpContext);
        var providers = await _security.GetEnabledExternalProvidersAsync();

        if (branding.RestrictExternalProviders)
        {
            var allowed = branding.EnabledProviderSchemes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            providers = providers.Where(x => allowed.Contains(x.Scheme)).ToArray();
        }

        return providers
            .Select(x => new ExternalProviderViewModel
            {
                DisplayName = x.DisplayName,
                Scheme = x.Scheme,
                ButtonText = x.ButtonText
            })
            .ToArray();
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string? returnUrl)
    {
        var branding = await _brandingResolver.ResolveAsync(HttpContext);
        return new LoginViewModel
        {
            ReturnUrl = returnUrl,
            EnableLocalLogin = branding.EnableLocalLogin,
            ExternalProviders = await GetExternalProvidersAsync(returnUrl)
        };
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        return View(await BuildLoginViewModelAsync(returnUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ExternalProviders = await GetExternalProvidersAsync(model.ReturnUrl);
            return View(model);
        }

        var branding = await _brandingResolver.ResolveAsync(HttpContext);
        model.EnableLocalLogin = branding.EnableLocalLogin;
        if (!branding.EnableLocalLogin)
        {
            ModelState.AddModelError(string.Empty, _localizer["PasswordLoginDisabledForThisApplication"].Value);
            model.ExternalProviders = await GetExternalProvidersAsync(model.ReturnUrl);
            return View(model);
        }

        var result = await _localAccounts.PasswordSignInAsync(new LoginLocalAccountRequest(
            model.Email,
            model.Password,
            model.RememberMe,
            BuildEmailConfirmationUrl));

        switch (result.Status)
        {
            case LocalLoginStatus.RequiresTwoFactor:
                await RecordAuditAsync("Password Login", "Authentication", "MfaRequired", result.UserId, result.Email, _localizer["AuditPasswordValidatedMfaRequested"].Value, "Password");
                return RedirectToAction(nameof(LoginWith2fa), new { model.ReturnUrl, model.RememberMe });

            case LocalLoginStatus.Succeeded:
                if (result.UserId.HasValue)
                {
                    var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());
                    if (user is not null)
                    {
                        await RecordLoginAsync(user, "Password", "Success", _localizer["AuditPasswordLoginSucceeded"].Value);
                    }
                }
                return LocalRedirect(model.ReturnUrl ?? "/");

            case LocalLoginStatus.EmailNotConfirmed:
                ModelState.AddModelError(string.Empty, _localizer["EmailConfirmationRequiredBeforeLogin"].Value);
                model.ShowResendEmailConfirmation = true;
                if (!string.IsNullOrWhiteSpace(result.ConfirmationLink))
                {
                    ViewData["EmailConfirmationLink"] = result.ConfirmationLink;
                    ViewData["EmailDeliveryUnavailable"] = result.EmailDeliveryUnavailable;
                }
                await RecordAuditAsync("Password Login", "Authentication", "NotAllowed", result.UserId, result.Email, _localizer["EmailConfirmationRequiredBeforeLogin"].Value, "Password");
                break;

            case LocalLoginStatus.LockedOut:
                ModelState.AddModelError(string.Empty, _localizer["UserTemporarilyLockedInvalidAttempts"].Value);
                await RecordAuditAsync("Password Login", "Authentication", "LockedOut", result.UserId, result.Email, _localizer["UserTemporarilyLockedInvalidAttempts"].Value, "Password");
                break;

            case LocalLoginStatus.BlockedOrSuspended:
            case LocalLoginStatus.InvalidCredentials:
            default:
                ModelState.AddModelError(string.Empty, _localizer["InvalidUsernameOrPassword"].Value);
                await RecordAuditAsync("Password Login", "Authentication", "Failed", result.UserId, result.Email ?? model.Email, _localizer["AuditPasswordLoginFailed"].Value, "Password");
                break;
        }

        model.ExternalProviders = await GetExternalProvidersAsync(model.ReturnUrl);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login), new { returnUrl });

        return View(new TwoFactorLoginViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(TwoFactorLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login), new { model.ReturnUrl });

        var code = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            code,
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            await _security.UpsertMfaMethodAsync(user, "Authenticator", _localizer["AuthenticatorApp"].Value, true, true, DateTimeOffset.UtcNow);
            await RecordLoginAsync(user, "Password+MFA", "Success", _localizer["AuditPasswordMfaLoginSucceeded"].Value);
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            await RecordAuditAsync("MFA Login", "Authentication", "LockedOut", user.Id, user.Email, _localizer["AuditUserLockedByMfaFailures"].Value, "Authenticator");
            ModelState.AddModelError(string.Empty, _localizer["UserTemporarilyLockedInvalidAttempts"].Value);
            return View(model);
        }

        await RecordAuditAsync("MFA Login", "Authentication", "Failed", user.Id, user.Email, _localizer["AuditInvalidMfaCode"].Value, "Authenticator");
        ModelState.AddModelError(string.Empty, _localizer["InvalidCode"].Value);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LoginWithRecoveryCode(string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login), new { returnUrl });
        return View(new RecoveryCodeLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null) return RedirectToAction(nameof(Login), new { model.ReturnUrl });

        var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);
        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            await RecordLoginAsync(user, "RecoveryCode", "Success", _localizer["AuditRecoveryCodeLoginSucceeded"].Value);
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        await RecordAuditAsync("Recovery Code Login", "Authentication", result.IsLockedOut ? "LockedOut" : "Failed", user.Id, user.Email, _localizer["AuditRecoveryCodeLoginFailed"].Value, "RecoveryCode");
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? _localizer["UserTemporarilyLocked"].Value : _localizer["InvalidRecoveryCode"].Value);
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ManageMfa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await EnsureAuthenticatorKeyAsync(user);
        return View(await BuildMfaSetupViewModelAsync(user));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableAuthenticator(MfaSetupViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await EnsureAuthenticatorKeyAsync(user);

        var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, AuthenticatorProvider, verificationCode);
        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.Code), _localizer["InvalidAuthenticatorCodeCheckDeviceTime"].Value);
            return View(nameof(ManageMfa), await BuildMfaSetupViewModelAsync(user));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _security.UpsertMfaMethodAsync(user, "Authenticator", _localizer["AuthenticatorApp"].Value, true, true, DateTimeOffset.UtcNow);
        await RecordAuditAsync("Enable MFA", "Security", "Success", user.Id, user.Email, _localizer["AuditUserEnabledMfaAuthenticator"].Value, "Authenticator");

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return View("RecoveryCodes", new RecoveryCodesViewModel { Codes = codes?.ToArray() ?? Array.Empty<string>() });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableMfa()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _security.UpdateMfaMethodStatusAsync(user.Id, "Authenticator", false);
        await RecordAuditAsync("Disable MFA", "Security", "Success", user.Id, user.Email, _localizer["AuditUserDisabledMfa"].Value, "Authenticator");
        TempData["Success"] = _localizer["MfaDisabled"].Value;
        return RedirectToAction(nameof(ManageMfa));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetAuthenticator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _security.UpdateMfaMethodStatusAsync(user.Id, "Authenticator", false);
        await RecordAuditAsync("Reset Authenticator", "Security", "Success", user.Id, user.Email, _localizer["AuditUserResetAuthenticatorKey"].Value, "Authenticator");
        TempData["Success"] = _localizer["AuthenticatorKeyResetScanQrAgain"].Value;
        return RedirectToAction(nameof(ManageMfa));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            TempData["Error"] = _localizer["EnableMfaBeforeGeneratingRecoveryCodes"].Value;
            return RedirectToAction(nameof(ManageMfa));
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        await _security.UpsertMfaMethodAsync(user, "RecoveryCodes", _localizer["RecoveryCodes"].Value, true, false, null);
        await RecordAuditAsync("Generate Recovery Codes", "Security", "Success", user.Id, user.Email, _localizer["AuditUserGeneratedNewRecoveryCodes"].Value, "RecoveryCode");
        return View("RecoveryCodes", new RecoveryCodesViewModel { Codes = codes?.ToArray() ?? Array.Empty<string>() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!await _security.IsProviderEnabledAsync(provider))
        {
            TempData["Error"] = _localizer["ExternalProviderDisabledOrNotRegistered"].Value;
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var branding = await _brandingResolver.ResolveAsync(HttpContext);
        if (branding.RestrictExternalProviders && !branding.EnabledProviderSchemes.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = _localizer["ExternalProviderDisabledForThisApplication"].Value;
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            ModelState.AddModelError(string.Empty, string.Format(_localizer["ExternalProviderErrorFormat"].Value, remoteError));
            return View(nameof(Login), await BuildLoginViewModelAsync(returnUrl));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ModelState.AddModelError(string.Empty, _localizer["ExternalLoginInfoLoadFailed"].Value);
            return View(nameof(Login), await BuildLoginViewModelAsync(returnUrl));
        }

        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            var existingEmail = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (existingUser is null && !string.IsNullOrWhiteSpace(existingEmail))
            {
                existingUser = await _userManager.FindByEmailAsync(existingEmail);
            }

            if (existingUser is not null)
            {
                UpdateUserFromExternalProvider(existingUser, info.Principal);
                existingUser.EmailConfirmed = true;
                existingUser.LastLoginAt = DateTimeOffset.UtcNow;
                await _userManager.UpdateAsync(existingUser);
                await RecordLoginAsync(existingUser, info.LoginProvider, "Success", _localizer["AuditExternalLoginSucceeded"].Value);
            }

            return LocalRedirect(returnUrl ?? "/");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, _localizer["ExternalProviderDidNotReturnEmail"].Value);
            return View(nameof(Login), await BuildLoginViewModelAsync(returnUrl));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = GetExternalDisplayName(info.Principal, email),
                AvatarUrl = GetExternalAvatarUrl(info.Principal),
                Status = UserStatus.Active
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(nameof(Login), await BuildLoginViewModelAsync(returnUrl));
            }
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code.Contains("LoginAlreadyAssociated", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var error in addLoginResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(nameof(Login), await BuildLoginViewModelAsync(returnUrl));
        }

        UpdateUserFromExternalProvider(user, info.Principal);
        user.EmailConfirmed = true;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        await RecordLoginAsync(user, info.LoginProvider, "Success", _localizer["AuditExternalLoginLinkedOrSucceeded"].Value);
        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _localAccounts.RegisterAsync(new RegisterLocalAccountRequest(
            model.DisplayName,
            model.Email,
            model.Password,
            BuildEmailConfirmationUrl));

        if (result.Succeeded)
        {
            await RecordAuditAsync("Register", "Identity", "Success", result.UserId, result.Email, _localizer["AuditUserCreatedNewAccount"].Value, null);
            return View("EmailConfirmationSent", new EmailConfirmationSentViewModel
            {
                Email = result.Email,
                ConfirmationLink = result.ConfirmationLink,
                ReturnUrl = model.ReturnUrl
            });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EmailConfirmationSent(string email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var result = await _localAccounts.CreateEmailConfirmationLinkAsync(email, BuildEmailConfirmationUrl);
        if (!result.UserFound)
        {
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        return View(new EmailConfirmationSentViewModel
        {
            Email = result.Email,
            ConfirmationLink = result.Link,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendEmailConfirmation(LoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return RedirectToAction(nameof(Login), new { model.ReturnUrl });
        }

        var result = await _localAccounts.CreateEmailConfirmationLinkAsync(model.Email, BuildEmailConfirmationUrl);
        return View("EmailConfirmationSent", new EmailConfirmationSentViewModel
        {
            Email = result.Email,
            ConfirmationLink = result.Link,
            ReturnUrl = model.ReturnUrl
        });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return View("EmailConfirmationResult", new EmailConfirmationResultViewModel
            {
                Succeeded = false,
                Message = _localizer["InvalidEmailConfirmationLink"].Value
            });
        }

        var result = await _localAccounts.ConfirmEmailAsync(new ConfirmEmailRequest(parsedUserId, code));
        return View("EmailConfirmationResult", new EmailConfirmationResultViewModel
        {
            Succeeded = result.Succeeded,
            Message = result.Succeeded
                ? _localizer["EmailConfirmedSuccessfully"].Value
                : _localizer[result.InvalidLink ? "InvalidEmailConfirmationLink" : "EmailConfirmationFailed"].Value
        });
    }

    [HttpGet]
    public IActionResult ForgotPassword(string? returnUrl = null) => View(new ForgotPasswordViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _localAccounts.CreatePasswordResetLinkAsync(new ForgotPasswordRequest(
            model.Email,
            BuildPasswordResetUrl));

        if (result.UserFound)
        {
            var user = await _userManager.FindByEmailAsync(result.Email);
            await RecordAuditAsync("Forgot Password", "Identity", "ResetLinkGenerated", user?.Id, result.Email, _localizer["AuditPasswordResetLinkGenerated"].Value, "Password");
        }

        return View("PasswordResetSent", new PasswordResetSentViewModel
        {
            Email = result.Email,
            ResetLink = result.Link,
            ReturnUrl = model.ReturnUrl
        });
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email = null, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = _localizer["InvalidPasswordResetLink"].Value;
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _localAccounts.ResetPasswordAsync(new ResetPasswordRequest(
            model.Email,
            model.Code,
            model.Password));

        if (result.InvalidLink)
        {
            TempData["Error"] = _localizer["InvalidPasswordResetLink"].Value;
            return RedirectToAction(nameof(Login));
        }

        if (result.Succeeded)
        {
            await RecordAuditAsync("Reset Password", "Identity", "Success", result.UserId, result.Email, _localizer["AuditPasswordResetSucceeded"].Value, "Password");
            TempData["Success"] = _localizer["PasswordResetSuccessfully"].Value;
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied() => View();

    private static void UpdateUserFromExternalProvider(ApplicationUser user, ClaimsPrincipal principal)
    {
        var displayName = GetExternalDisplayName(principal, user.Email ?? user.UserName ?? user.DisplayName);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            user.DisplayName = displayName;
        }

        var avatarUrl = GetExternalAvatarUrl(principal);
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string GetExternalDisplayName(ClaimsPrincipal principal, string fallback)
    {
        return principal.FindFirstValue(ClaimTypes.Name)
               ?? principal.FindFirstValue("name")
               ?? principal.FindFirstValue("urn:github:name")
               ?? principal.Identity?.Name
               ?? fallback;
    }

    private static string? GetExternalAvatarUrl(ClaimsPrincipal principal)
    {
        var possibleClaimTypes = new[]
        {
            "picture",
            "urn:google:picture",
            "urn:microsoft:picture",
            "avatar_url",
            "urn:github:avatar_url",
            "urn:github:avatar",
            "profile_image_url",
            "image"
        };

        foreach (var claimType in possibleClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (IsValidExternalImageUrl(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsValidExternalImageUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private async Task<MfaSetupViewModel> BuildMfaSetupViewModelAsync(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? user.Id.ToString();
        var authenticatorUri = GenerateQrCodeUri(email, key);
        return new MfaSetupViewModel
        {
            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            HasAuthenticator = !string.IsNullOrWhiteSpace(key),
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            SharedKey = FormatKey(key),
            AuthenticatorUri = authenticatorUri,
            QrCodeDataUrl = GenerateQrCodeDataUrl(authenticatorUri)
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

    private string BuildEmailConfirmationUrl(Guid userId, string code)
    {
        return Url.Action(
                   nameof(ConfirmEmail),
                   "Account",
                   new { userId = userId.ToString(), code },
                   Request.Scheme)
               ?? string.Empty;
    }

    private string BuildPasswordResetUrl(string email, string code)
    {
        return Url.Action(
                   nameof(ResetPassword),
                   "Account",
                   new { email, code },
                   Request.Scheme)
               ?? string.Empty;
    }

    private Task RecordLoginAsync(ApplicationUser user, string provider, string outcome, string description) =>
        _security.RecordLoginAsync(user, provider, outcome, description, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

    private Task RecordAuditAsync(string eventType, string category, string outcome, Guid? userId, string? userEmail, string description, string? provider) =>
        _security.RecordAuditAsync(eventType, category, outcome, userId, userEmail, description, provider, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

    private static string ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Dispositivo desconhecido";
        var os = ParseOperatingSystem(userAgent);
        var browser = ParseBrowser(userAgent);
        return $"{browser} em {os}";
    }

    private static string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Desconhecido";
        if (userAgent.Contains("Edg/")) return "Microsoft Edge";
        if (userAgent.Contains("Chrome/")) return "Chrome";
        if (userAgent.Contains("Firefox/")) return "Firefox";
        if (userAgent.Contains("Safari/")) return "Safari";
        return "Navegador";
    }

    private static string ParseOperatingSystem(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Desconhecido";
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac OS")) return "macOS";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
        if (userAgent.Contains("Linux")) return "Linux";
        return "Sistema";
    }
}

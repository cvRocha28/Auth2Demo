using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.ClientManager)]
public sealed class ApplicationSecretsAuditController : Controller
{
    private readonly IApplicationSecretsAuditService _applicationSecrets;

    public ApplicationSecretsAuditController(IApplicationSecretsAuditService applicationSecrets)
    {
        _applicationSecrets = applicationSecrets;
    }

    public async Task<IActionResult> Index(bool activeOnly = false)
    {
        var secrets = await _applicationSecrets.ListAsync(activeOnly);

        ViewData["ActiveOnly"] = activeOnly;
        return View(secrets.Select(secret => new ApplicationSecretAuditListItemViewModel
        {
            Id = secret.Id,
            ApplicationId = secret.ApplicationId,
            ClientId = secret.ClientId,
            Description = secret.Description,
            SecretPrefix = secret.SecretPrefix,
            CreatedAtUtc = secret.CreatedAtUtc,
            ExpiresAtUtc = secret.ExpiresAtUtc,
            RevokedAtUtc = secret.RevokedAtUtc,
            RevokedReason = secret.RevokedReason
        }).ToList());
    }
}

using Auth2Demo.Application.Common.Abstractions;
using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Infrastructure.Services.Portal;
using Auth2Demo.Infrastructure.Services.Admin;
using Auth2Demo.Infrastructure.Repositories.Portal;
using Auth2Demo.Infrastructure.Repositories.Admin;
using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Infrastructure.Identity.Email;
using Auth2Demo.Infrastructure.Identity.Services;
using Auth2Demo.Infrastructure.Persistence;
using Auth2Demo.Infrastructure.Security;
using Auth2Demo.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth2Demo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não foi configurada.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>()
            .SetApplicationName("Auth2Demo.IdentityServer");

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IApplicationInitializer, DatabaseInitializer>();

        services.AddSingleton<IClientSecretGenerator, ClientSecretGenerator>();
        services.AddSingleton<IIdentityProviderSecretProtector, IdentityProviderSecretProtector>();

        services.AddScoped<ILocalAccountService, LocalAccountService>();
        services.AddScoped<IIdentityEmailSender, NullIdentityEmailSender>();

        services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
        services.AddScoped<IAdminBrandingRepository, AdminBrandingRepository>();
        services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
        services.AddScoped<IAdminDeviceRepository, AdminDeviceRepository>();
        services.AddScoped<IAdminEmailTemplateRepository, AdminEmailTemplateRepository>();
        services.AddScoped<IAdminHealthRepository, AdminHealthRepository>();
        services.AddScoped<IAdminSecuritySettingsRepository, AdminSecuritySettingsRepository>();
        services.AddScoped<IAdminSessionRepository, AdminSessionRepository>();
        services.AddScoped<IAdminPasskeyRepository, AdminPasskeyRepository>();
        services.AddScoped<IAdminPermissionRepository, AdminPermissionRepository>();
        services.AddScoped<IAdminIdentityProviderRepository, AdminIdentityProviderRepository>();
        services.AddScoped<IAdminMfaRepository, AdminMfaRepository>();
        services.AddScoped<IAdminOpenIddictMetricsRepository, AdminOpenIddictMetricsRepository>();
        services.AddScoped<IAdminRoleRepository, AdminRoleRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IScopeAdminRepository, ScopeAdminRepository>();
        services.AddScoped<IApplicationAuditRepository, ApplicationAuditRepository>();
        services.AddScoped<IApplicationSecretsAuditRepository, ApplicationSecretsAuditRepository>();
        services.AddScoped<IClientApplicationRepository, ClientApplicationRepository>();
        services.AddScoped<IApplicationSecretRepository, ApplicationSecretRepository>();
        services.AddScoped<IExternalProviderRepository, ExternalProviderRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IAccountSecurityRepository, AccountSecurityRepository>();

        services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
        services.AddScoped<IAdminBrandingService, AdminBrandingService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminDeviceService, AdminDeviceService>();
        services.AddScoped<IAdminEmailTemplateService, AdminEmailTemplateService>();
        services.AddScoped<IAdminHealthService, AdminHealthService>();
        services.AddScoped<IAdminSecuritySettingsService, AdminSecuritySettingsService>();
        services.AddScoped<IAdminSessionService, AdminSessionService>();
        services.AddScoped<IAdminPasskeyService, AdminPasskeyService>();
        services.AddScoped<IAdminPermissionService, AdminPermissionService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminIdentityProviderService, AdminIdentityProviderService>();
        services.AddScoped<IAdminMfaService, AdminMfaService>();
        services.AddScoped<IApplicationAuditService, ApplicationAuditService>();
        services.AddScoped<IApplicationSecretsAuditService, ApplicationSecretsAuditService>();
        services.AddScoped<IClientApplicationAdminService, ClientApplicationAdminService>();
        services.AddScoped<IScopeAdminService, ScopeAdminService>();
        services.AddScoped<IExternalProviderService, ExternalProviderService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IAccountSecurityService, AccountSecurityService>();

        return services;
    }
}
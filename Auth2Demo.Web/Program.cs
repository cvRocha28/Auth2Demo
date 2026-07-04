using Auth2Demo.Web;
using Auth2Demo.Application;
using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Infrastructure;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Auth2Demo.Infrastructure.Repositories.Admin;
using Auth2Demo.Infrastructure.Repositories.Portal;
using Auth2Demo.Web.Security;
using Auth2Demo.Web.Localization;
using Auth2Demo.Web.Seeding;
using Auth2Demo.Infrastructure.Services.Admin;
using Auth2Demo.Infrastructure.Services.Portal;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResource));
    });
builder.Services.AddRazorPages();


builder.Services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
builder.Services.AddScoped<IAdminBrandingRepository, AdminBrandingRepository>();
builder.Services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
builder.Services.AddScoped<IAdminDeviceRepository, AdminDeviceRepository>();
builder.Services.AddScoped<IAdminEmailTemplateRepository, AdminEmailTemplateRepository>();
builder.Services.AddScoped<IAdminHealthRepository, AdminHealthRepository>();
builder.Services.AddScoped<IAdminSecuritySettingsRepository, AdminSecuritySettingsRepository>();
builder.Services.AddScoped<IAdminSessionRepository, AdminSessionRepository>();
builder.Services.AddScoped<IAdminPasskeyRepository, AdminPasskeyRepository>();
builder.Services.AddScoped<IAdminPermissionRepository, AdminPermissionRepository>();
builder.Services.AddScoped<IAdminIdentityProviderRepository, AdminIdentityProviderRepository>();
builder.Services.AddScoped<IAdminMfaRepository, AdminMfaRepository>();
builder.Services.AddScoped<IExternalProviderRepository, ExternalProviderRepository>();
builder.Services.AddScoped<IPerfilRepository, PerfilRepository>();
builder.Services.AddScoped<IAccountSecurityRepository, AccountSecurityRepository>();

builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddScoped<IAdminBrandingService, AdminBrandingService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminDeviceService, AdminDeviceService>();
builder.Services.AddScoped<IAdminEmailTemplateService, AdminEmailTemplateService>();
builder.Services.AddScoped<IAdminHealthService, AdminHealthService>();
builder.Services.AddScoped<IAdminSecuritySettingsService, AdminSecuritySettingsService>();
builder.Services.AddScoped<IAdminSessionService, AdminSessionService>();
builder.Services.AddScoped<IAdminPasskeyService, AdminPasskeyService>();
builder.Services.AddScoped<IAdminPermissionService, AdminPermissionService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminIdentityProviderService, AdminIdentityProviderService>();
builder.Services.AddScoped<IAdminMfaService, AdminMfaService>();
builder.Services.AddScoped<IExternalProviderService, ExternalProviderService>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.Cookie.Name = "Auth2Demo.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});

builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>, ExternalProviderOptionsSetup>();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<Microsoft.AspNetCore.Authentication.MicrosoftAccount.MicrosoftAccountOptions>, ExternalProviderOptionsSetup>();

builder.Services.AddAuthentication()
    .AddGoogle("Google", _ => { })
    .AddMicrosoftAccount("Microsoft", _ => { });

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>()
            .ReplaceDefaultEntities<Guid>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetUserInfoEndpointUris("/connect/userinfo")
            .SetEndSessionEndpointUris("/connect/logout")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetRevocationEndpointUris("/connect/revoke");

        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow()
            .AllowClientCredentialsFlow();

        options.RequireProofKeyForCodeExchange();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess,
            "auth2demo.api",
            "rareles.api");

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Admin, policy =>
        policy.RequireRole(AuthRoles.Admin));

    options.AddPolicy(AuthPolicies.ClientManager, policy =>
        policy.RequireRole(AuthRoles.Admin, AuthRoles.ClientManager));

    options.AddPolicy(AuthPolicies.UserManager, policy =>
        policy.RequireRole(AuthRoles.Admin, AuthRoles.UserManager));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

var supportedCultures = new[]
{
    new CultureInfo("pt-BR"),
    new CultureInfo("en-US")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders = new RequestCultureProvider[]
{
    new UserProfileRequestCultureProvider(),
    new CookieRequestCultureProvider(),
    new QueryStringRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
};

app.UseRouting();
app.UseAuthentication();
app.UseRequestLocalization(localizationOptions);
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await IdentityServerSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await app.RunAsync();

public partial class Program
{
}

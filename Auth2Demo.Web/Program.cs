using Auth2Demo.Web;
using Auth2Demo.Application;
using Auth2Demo.Application.Common.Abstractions;
using Auth2Demo.Infrastructure;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Security;
using Auth2Demo.Web.Localization;
using Auth2Demo.Web.Services.Branding;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuth2DemoIdentityServer();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResource));
    });
builder.Services.AddRazorPages();
builder.Services.AddScoped<IBrandingResolver, BrandingResolver>();


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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Admin, policy =>
        policy.RequireRole(Auth2Demo.Application.Security.AuthRoles.Admin));

    options.AddPolicy(AuthPolicies.ClientManager, policy =>
        policy.RequireRole(Auth2Demo.Application.Security.AuthRoles.Admin, Auth2Demo.Application.Security.AuthRoles.ClientManager));

    options.AddPolicy(AuthPolicies.UserManager, policy =>
        policy.RequireRole(Auth2Demo.Application.Security.AuthRoles.Admin, Auth2Demo.Application.Security.AuthRoles.UserManager));
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
    var initializer = scope.ServiceProvider.GetRequiredService<IApplicationInitializer>();
    await initializer.InitializeAsync();
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

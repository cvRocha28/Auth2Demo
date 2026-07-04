using Auth2Demo.Application.Common.Abstractions;
using Auth2Demo.Application.Services.Identity;
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

        services.AddSingleton<IClientSecretGenerator, ClientSecretGenerator>();

        services.AddScoped<ILocalAccountService, LocalAccountService>();
        services.AddScoped<IIdentityEmailSender, NullIdentityEmailSender>();

        return services;
    }
}
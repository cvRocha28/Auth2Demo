using Auth2Demo.Application.Common.Abstractions;
using Auth2Demo.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth2Demo.Infrastructure.Persistence;

public sealed class DatabaseInitializer : IApplicationInitializer
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public DatabaseInitializer(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();
        await IdentityServerSeeder.SeedAsync(scope.ServiceProvider, _configuration);
    }
}

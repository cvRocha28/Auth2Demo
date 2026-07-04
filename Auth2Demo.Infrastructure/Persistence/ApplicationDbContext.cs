using Auth2Demo.Application.Common.Abstractions;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Domain.Security;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Auth2Demo.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<IdentityProvider> IdentityProviders => Set<IdentityProvider>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<MfaMethod> MfaMethods => Set<MfaMethod>();
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<BrandingSettings> BrandingSettings => Set<BrandingSettings>();
    public DbSet<SecuritySettings> SecuritySettings => Set<SecuritySettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict<Guid>();

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        ConfigureIdentityTableNames(builder);
        ConfigureOpenIddictTableNames(builder);
        ConfigureInfrastructureTableNames(builder);
        ConfigurePortalEntities(builder);
    }

    private static void ConfigureIdentityTableNames(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("IdentityUsers");
        builder.Entity<ApplicationRole>().ToTable("IdentityRoles");

        builder.Entity<IdentityUserRole<Guid>>().ToTable("IdentityUserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("IdentityUserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("IdentityUserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("IdentityUserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("IdentityRoleClaims");
    }

    private static void ConfigureOpenIddictTableNames(ModelBuilder builder)
    {
        builder.Entity<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .ToTable("IdentityApplications");

        builder.Entity<OpenIddictEntityFrameworkCoreAuthorization<Guid>>()
            .ToTable("IdentityAuthorizations");

        builder.Entity<OpenIddictEntityFrameworkCoreScope<Guid>>()
            .ToTable("IdentityScopes");

        builder.Entity<OpenIddictEntityFrameworkCoreToken<Guid>>()
            .ToTable("IdentityTokens");
    }

    private static void ConfigureInfrastructureTableNames(ModelBuilder builder)
    {
        builder.Entity<DataProtectionKey>()
            .ToTable("IdentityDataProtectionKeys");
    }

    private static void ConfigurePortalEntities(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("IdentityAuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Outcome).HasMaxLength(40).IsRequired();
            entity.Property(x => x.UserEmail).HasMaxLength(256);
            entity.Property(x => x.IpAddress).HasMaxLength(80);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.Provider).HasMaxLength(80);
            entity.HasIndex(x => new { x.CreatedAt, x.Category });
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("IdentityUserSessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.SessionId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DeviceName).HasMaxLength(160);
            entity.Property(x => x.IpAddress).HasMaxLength(80);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.HasIndex(x => new { x.UserId, x.IsRevoked });
        });

        builder.Entity<UserDevice>(entity =>
        {
            entity.ToTable("IdentityUserDevices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Browser).HasMaxLength(120);
            entity.Property(x => x.OperatingSystem).HasMaxLength(120);
            entity.Property(x => x.IpAddress).HasMaxLength(80);
            entity.HasIndex(x => new { x.UserId, x.IsTrusted });
        });

        builder.Entity<MfaMethod>(entity =>
        {
            entity.ToTable("IdentityMfaMethods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        });

        builder.Entity<PasskeyCredential>(entity =>
        {
            entity.ToTable("IdentityPasskeys");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CredentialId).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.CredentialId).IsUnique();
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("IdentityPermissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("IdentityRolePermissions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        });

        builder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("IdentityEmailTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(250).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        builder.Entity<BrandingSettings>(entity =>
        {
            entity.ToTable("IdentityBrandingSettings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PrimaryColor).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SecondaryColor).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Theme).HasMaxLength(40).IsRequired();
        });

        builder.Entity<SecuritySettings>(entity =>
        {
            entity.ToTable("IdentitySecuritySettings");
            entity.HasKey(x => x.Id);
        });
    }
}


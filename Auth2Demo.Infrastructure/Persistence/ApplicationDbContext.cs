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
    public DbSet<Company> Companies => Set<Company>();
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
    public DbSet<IdentityApplicationSecret> IdentityApplicationSecrets => Set<IdentityApplicationSecret>();
    public DbSet<ApplicationTenantAssignment> ApplicationTenantAssignments => Set<ApplicationTenantAssignment>();
    public DbSet<ApplicationIdentityProvider> ApplicationIdentityProviders => Set<ApplicationIdentityProvider>();
    public DbSet<ApplicationUserAssignment> ApplicationUserAssignments => Set<ApplicationUserAssignment>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<CompanyGroup> CompanyGroups => Set<CompanyGroup>();
    public DbSet<CompanyGroupMember> CompanyGroupMembers => Set<CompanyGroupMember>();
    public DbSet<EnterpriseApplicationRole> EnterpriseApplicationRoles => Set<EnterpriseApplicationRole>();
    public DbSet<EnterpriseApplicationAssignment> EnterpriseApplicationAssignments => Set<EnterpriseApplicationAssignment>();

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
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("IdentityUsers");
            entity.Property(x => x.Language).HasMaxLength(16);
            entity.Property(x => x.Culture).HasMaxLength(20);
            entity.Property(x => x.Country).HasMaxLength(8);
            entity.Property(x => x.Locale).HasMaxLength(20);
            entity.Property(x => x.TimeZone).HasMaxLength(120);
            entity.HasIndex(x => x.CompanyId);
            entity.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<ApplicationRole>().ToTable("IdentityRoles");

        builder.Entity<IdentityUserRole<Guid>>().ToTable("IdentityUserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("IdentityUserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("IdentityUserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("IdentityUserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("IdentityRoleClaims");
    }

    private static void ConfigureOpenIddictTableNames(ModelBuilder builder)
    {
        builder.Entity<OpenIddictEntityFrameworkCoreApplication<Guid>>(entity =>
        {
            entity.ToTable("IdentityApplications");
            entity.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<DateTimeOffset?>("UpdatedAt")
                .HasColumnType("datetimeoffset");
            entity.Property<bool>("IsDeleted")
                .HasColumnType("bit")
                .HasDefaultValue(false);
            entity.Property<DateTimeOffset?>("DeletedAt")
                .HasColumnType("datetimeoffset");
            entity.Property<Guid?>("CreatedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid?>("UpdatedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<Guid?>("DeletedByUserId")
                .HasColumnType("uniqueidentifier");
            entity.Property<bool>("IsEnabled")
                .HasColumnType("bit")
                .HasDefaultValue(true);
        });

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

        builder.Entity<Company>(entity =>
        {
            entity.ToTable("IdentityCompanies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.DomainHint).HasMaxLength(160);
            entity.Property(x => x.Country).HasMaxLength(8);
            entity.Property(x => x.Culture).HasMaxLength(20);
            entity.Property(x => x.TimeZone).HasMaxLength(120);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.DomainHint);
        });

        builder.Entity<IdentityProvider>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.SortOrder });
            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OpenIddictEntityFrameworkCoreApplication<Guid>>(entity =>
        {
            entity.Property<Guid?>("CompanyId").HasColumnType("uniqueidentifier");
            entity.HasIndex("CompanyId");
        });

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



        builder.Entity<ApplicationTenantAssignment>(entity =>
        {
            entity.ToTable("IdentityApplicationTenantAssignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.ApplicationId, x.CompanyId }).IsUnique();
            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationIdentityProvider>(entity =>
        {
            entity.ToTable("IdentityApplicationProviders");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ApplicationId, x.IdentityProviderId }).IsUnique();
            entity.HasOne(x => x.IdentityProvider)
                .WithMany()
                .HasForeignKey(x => x.IdentityProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUserAssignment>(entity =>
        {
            entity.ToTable("IdentityApplicationUserAssignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role).HasMaxLength(120);
            entity.HasIndex(x => new { x.ApplicationId, x.UserId }).IsUnique();
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompanyUser>(entity =>
        {
            entity.ToTable("IdentityCompanyUsers"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Department).HasMaxLength(160); entity.Property(x => x.JobTitle).HasMaxLength(160);
            entity.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CompanyGroup>(entity =>
        {
            entity.ToTable("IdentityCompanyGroups"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CompanyGroupMember>(entity =>
        {
            entity.ToTable("IdentityCompanyGroupMembers"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<EnterpriseApplicationRole>(entity =>
        {
            entity.ToTable("IdentityEnterpriseApplicationRoles"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired(); entity.Property(x => x.Value).HasMaxLength(120).IsRequired(); entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.ApplicationId, x.Value }).IsUnique();
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<EnterpriseApplicationAssignment>(entity =>
        {
            entity.ToTable("IdentityEnterpriseApplicationAssignments"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ApplicationId, x.CompanyId, x.PrincipalType, x.UserId, x.GroupId }).IsUnique();
            entity.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            // SQL Server rejects SET NULL here because Application -> Assignment and
            // Application -> Role -> Assignment would create multiple cascading paths.
            // Role deletion is therefore explicit and governed by the application service.
            entity.HasOne(x => x.ApplicationRole)
                .WithMany()
                .HasForeignKey(x => x.ApplicationRoleId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(t => t.HasCheckConstraint("CK_EnterpriseAssignment_Principal", "([PrincipalType] = 1 AND [UserId] IS NOT NULL AND [GroupId] IS NULL) OR ([PrincipalType] = 2 AND [UserId] IS NULL AND [GroupId] IS NOT NULL)"));
        });

        builder.Entity<IdentityApplicationSecret>(entity =>
        {
            entity.ToTable("IdentityApplicationSecrets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.SecretHash).IsRequired();
            entity.Property(x => x.SecretPrefix).HasMaxLength(16).IsRequired();
            entity.Property(x => x.RevokedReason).HasMaxLength(300);
            entity.HasIndex(x => x.ApplicationId);
            entity.HasIndex(x => new { x.ApplicationId, x.RevokedAtUtc, x.ExpiresAtUtc });
            entity.HasOne<OpenIddictEntityFrameworkCoreApplication<Guid>>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(x => x.IsRevoked);
            entity.Ignore(x => x.IsExpired);
            entity.Ignore(x => x.IsActive);
        });
    }
}


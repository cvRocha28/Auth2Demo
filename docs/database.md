# Database and migrations

Auth2Demo uses Entity Framework Core with SQL Server. `ApplicationDbContext` combines ASP.NET Core Identity, OpenIddict, Data Protection, tenant governance, and administrative entities.

## Table groups

### ASP.NET Core Identity

```text
IdentityUsers
IdentityRoles
IdentityUserRoles
IdentityUserClaims
IdentityUserLogins
IdentityUserTokens
IdentityRoleClaims
```

`IdentityUsers` also stores optional language, culture, country, locale, time-zone, and company-related profile data.

### OpenIddict

```text
IdentityApplications
IdentityAuthorizations
IdentityScopes
IdentityTokens
```

Auth2Demo adds shadow metadata to applications, including enabled state, timestamps, deletion metadata, and actor identifiers.

### Data Protection

```text
IdentityDataProtectionKeys
```

### Tenant governance

```text
IdentityCompanies
IdentityProviders
IdentityApplicationTenantAssignments
IdentityApplicationIdentityProviders
IdentityCompanyUsers
IdentityCompanyGroups
IdentityCompanyGroupMembers
IdentityEnterpriseApplicationRoles
IdentityEnterpriseApplicationAssignments
```

### Security and administration

```text
IdentityAuditLogs
IdentityUserSessions
IdentityUserDevices
IdentityMfaMethods
IdentityPasskeyCredentials
IdentityPermissions
IdentityRolePermissions
IdentityEmailTemplates
IdentityBrandingSettings
IdentitySecuritySettings
IdentityApplicationSecrets
```

The exact names are defined in EF mappings and should be verified in each generated migration.

## Important constraints

- Company group names are unique per company.
- A user can only appear once in the same group.
- Enterprise role values are unique per application.
- Enterprise assignment uniqueness prevents duplicate principal assignments.
- A check constraint enforces either a user principal or a group principal, never both.
- Provider, membership, and assignment relationships use explicit delete behaviors.

## SQL Server cascade-path rule

SQL Server rejects some graphs with more than one cascading path. The enterprise assignment to application-role relationship uses `NoAction`, while application-to-assignment and application-to-role relationships may cascade.

Application role deletion must therefore explicitly remove or update related assignments before deleting the role.

## Migration ownership

Migrations belong to `Auth2Demo.Infrastructure/Persistence/Migrations`.

Create a migration:

```powershell
dotnet ef migrations add MigrationName `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web `
  --output-dir Persistence\Migrations
```

Apply migrations:

```powershell
dotnet ef database update `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web
```

Remove the most recent unapplied migration:

```powershell
dotnet ef migrations remove `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web
```

Generate a reviewable SQL script:

```powershell
dotnet ef migrations script --idempotent `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web `
  --output Auth2Demo.sql
```

## Migration review checklist

Before applying a migration:

- inspect every foreign key and delete action;
- confirm indexes for common tenant, user, client, and status filters;
- confirm unique indexes match business rules;
- check nullable and maximum-length changes;
- look for unintended table drops or column renames;
- inspect default constraints and UTC timestamp expressions;
- test against an empty database;
- test upgrade from the current production schema;
- back up production data.

## Database initialization

`DatabaseInitializer` runs through `IApplicationInitializer` at application startup. Seed operations must be idempotent and should never reset production credentials or overwrite administrator changes.

# Testing

The solution contains unit and integration test projects, but comprehensive identity-platform coverage should be expanded continuously.

## Test projects

```text
Auth2Demo.UnitTests
Auth2Demo.IntegrationTests
```

## Unit test targets

Prioritize deterministic tests for:

- company membership invariants;
- default-company behavior;
- group membership validation;
- enterprise assignment principal rules;
- tenant access evaluation;
- role aggregation;
- password policy mapping;
- secret expiration and revocation state;
- provider and tenant eligibility.

## Integration test targets

Use the hosted Web application and an isolated database to test:

- registration and login;
- profile password creation and change;
- effective security policy;
- authorization code with PKCE;
- client credentials;
- invalid redirect URIs;
- disabled clients and providers;
- multi-tenant external login;
- mandatory assignment allow and deny cases;
- direct user and group role assignments;
- tenant directory CRUD and cleanup;
- application role deletion behavior;
- admin authorization policies;
- antiforgery enforcement;
- culture selection.

## Database compatibility tests

Because SQL Server delete-path behavior differs from some in-memory providers, governance persistence tests should run against SQL Server or a SQL Server container. Do not rely exclusively on EF Core InMemory for relational constraints.

## Validation workflow

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

For model changes:

```powershell
dotnet ef migrations add ValidationMigration `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web `
  --output-dir Persistence\Migrations

dotnet ef migrations script `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web
```

Inspect the migration, test it on an empty database, and remove the temporary migration when it is not intended for commit.

## Test data

- Never use real production personal data or secrets.
- Generate deterministic tenant, user, group, provider, and application fixtures.
- Keep test client secrets isolated.
- Reset databases between tests or use transaction isolation where supported.

# Deployment and operations

## Prerequisites

- .NET 10 runtime or hosting bundle.
- SQL Server or Azure SQL.
- HTTPS endpoint and trusted certificate.
- Persistent Data Protection storage.
- Production signing/encryption key strategy.
- Secure configuration and secret storage.

## Build and publish

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet publish Auth2Demo.Web --configuration Release --output publish
```

## Database deployment

Generate and review an idempotent migration script:

```powershell
dotnet ef migrations script --idempotent `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web `
  --output Auth2Demo.sql
```

Prefer a controlled deployment step over allowing the web process to make unrestricted production schema changes.

## Reverse proxy

Auth2Demo reads `X-Forwarded-For` and `X-Forwarded-Proto`. In production:

- configure the proxy to overwrite, not append untrusted values;
- restrict ASP.NET Core known proxies/networks;
- preserve the public HTTPS scheme and host;
- test `/signin-*`, `/connect/authorize`, `/connect/token`, and logout callbacks.

Incorrect forwarded headers commonly produce HTTP redirect URIs behind an HTTPS proxy.

## IIS

For IIS deployment:

- install the .NET 10 Hosting Bundle;
- use an application pool without managed CLR;
- grant the application identity only required file and certificate access;
- store secrets outside `web.config` when possible;
- enable stdout logs only during troubleshooting and remove them afterward;
- configure application initialization and health probes.

## Multiple instances

All instances must share:

- the same database schema;
- the same Data Protection key ring and application name;
- compatible signing and encryption keys;
- synchronized time;
- consistent public issuer and callback URLs.

Avoid in-memory state for security decisions that must survive restarts or scale-out.

## Health and monitoring

Monitor:

- process availability and restart count;
- database connectivity and latency;
- sign-in success/failure rates;
- OpenIddict endpoint errors;
- token issuance latency;
- external provider failures;
- Data Protection key access;
- expiring client/provider secrets;
- assignment-based access denials;
- audit ingestion failures.

## Backup and recovery

Back up:

- SQL Server databases;
- signing/encryption key material;
- provider-secret protection keys;
- deployment configuration;
- certificates and renewal procedures.

Test restore procedures regularly. A database restore without the matching protection keys can make stored secrets or cookies unusable.

## Production checklist

- [ ] Release build and tests pass.
- [ ] Migration SQL reviewed and backed up.
- [ ] HTTPS and public issuer verified.
- [ ] Redirect URIs verified for every client/provider.
- [ ] Development seed credentials removed.
- [ ] Admin MFA enforced.
- [ ] Trusted proxies explicitly configured.
- [ ] Signing and encryption keys are production-grade.
- [ ] Data Protection keys are shared and backed up.
- [ ] Secrets are loaded from a secure store.
- [ ] Rate limiting and security headers configured.
- [ ] Audit retention and alerting configured.
- [ ] Health checks and rollback plan tested.

# Security

Auth2Demo is security-sensitive software. This document records implemented controls and the minimum hardening required before production use.

## Implemented controls

- ASP.NET Core Identity account management.
- OpenIddict protocol validation and persistence.
- Role-based administrative authorization policies.
- Persisted password and lockout settings.
- Dynamic password validation across account and profile flows.
- Hashed client-secret storage with lifecycle metadata.
- Protected external-provider secrets.
- Persistent ASP.NET Core Data Protection keys.
- HTTPS redirection and secure cookies.
- Antiforgery protection for browser-based state changes.
- Tenant, provider, membership, group, and assignment validation.
- Relational constraints and unique indexes for governance data.
- Audit, session, and device administration models.

## Production hardening checklist

### Secrets and keys

- Store connection strings and root protection keys in a secret manager.
- Use a certificate, HSM, or managed key service for signing/encryption keys.
- Do not use development signing certificates in production.
- Rotate client and provider secrets.
- Restrict database access by managed identity or least-privilege credentials.

### Network and hosting

- Terminate TLS with a trusted certificate.
- Restrict forwarded headers to known proxies.
- Enable HSTS with an appropriate lifetime.
- Add CSP, frame-ancestors, X-Content-Type-Options, Referrer-Policy, and Permissions-Policy.
- Restrict administrative routes by network and strong authentication where possible.

### Authentication

- Enforce MFA for administrators.
- Enable lockout and rate limiting for login, registration, password reset, and external callbacks.
- Add breached-password detection or password block lists.
- Review account enumeration in all error messages.
- Require recent authentication for sensitive profile and admin operations.
- Provide secure recovery and administrator reset workflows.

### OAuth/OIDC

- Require exact redirect URI matching.
- Prefer Authorization Code with PKCE.
- Disable unused grant types and endpoints per client.
- Use short-lived access tokens and controlled refresh-token rotation.
- Validate issuer, audience, tenant, provider, nonce, state, and PKCE.
- Review consent and claim release per client.
- Never expose token values in administrative pages or logs.

### Multi-tenant authorization

- Revalidate tenant ownership on every state-changing request.
- Never trust `companyId`, `applicationId`, `groupId`, or `userId` solely because they came from the UI.
- Require active memberships, groups, providers, assignments, and roles.
- Deny by default when configuration is incomplete or ambiguous.
- Audit denied cross-tenant operations.

### Data and auditing

- Define audit retention and tamper protection.
- Minimize personal data and document retention periods.
- Encrypt backups and test restore procedures.
- Avoid storing unnecessary external claims.
- Implement privacy and deletion workflows appropriate to applicable law.

## Security settings propagation

The policy configured in `/Admin/SecuritySettings` must affect:

- registration;
- administrator user creation;
- local password creation for external users;
- password change in `/Perfil`;
- password reset;
- any future API that writes passwords.

The server-side Identity validator is the source of truth. UI requirements and checklists should be generated from the same policy but are not security controls on their own.

## Incident readiness

At minimum, record and correlate:

- successful and failed sign-ins;
- provider and tenant used;
- application/client ID;
- assignment-based access denial;
- role and permission changes;
- secret creation and revocation;
- security setting changes;
- session revocation;
- administrator actor and target IDs.

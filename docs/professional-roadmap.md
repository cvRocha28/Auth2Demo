# Professional roadmap

This roadmap distinguishes implemented foundations from recommended production milestones.

## Implemented foundations

### Protocol and applications

- OpenIddict-backed applications, authorizations, scopes, and tokens.
- Authorization Code with PKCE.
- Client Credentials and refresh-token scenarios.
- App registration administration.
- Redirect URI and permission management.
- Client secret lifecycle metadata and audit views.
- Per-client branding and authentication method configuration.

### Multi-tenant governance

- Companies as internal tenants.
- Multi-company user memberships.
- Tenant-owned groups and group members.
- Tenant-owned external identity providers.
- Allowed tenants and providers per enterprise application.
- Optional mandatory assignment.
- Direct user and group assignments.
- Application roles and access evaluation.

### Identity and security

- Local accounts and external login.
- Persisted security settings.
- Password-policy propagation to registration and profile flows.
- Sessions, devices, MFA, and passkey administration models/screens.
- Persistent Data Protection keys.
- Audit-oriented administration areas.

### Administration and experience

- Professional workspace-oriented admin navigation.
- Global and tenant-scoped directory screens.
- Enterprise application configuration.
- Localization in `pt-BR` and `en-US`.
- Branding and email template administration.

## Priority 1: correctness and automated coverage

- Replace sample tests with domain, service, and protocol tests.
- Add SQL Server integration tests for cascade behavior and unique constraints.
- Add end-to-end tests for tenant assignment decisions.
- Add concurrency handling for administration updates.
- Add systematic audit events for all governance changes.
- Eliminate all compiler and resource warnings.

## Priority 2: protocol and key hardening

- Use explicit production signing and encryption certificates or managed keys.
- Document and test discovery metadata.
- Add token lifetime and refresh-token rotation policies.
- Add revocation and cleanup jobs.
- Add DPoP or sender-constrained token research where applicable.
- Add PAR/JAR support assessment for higher-security clients.

## Priority 3: identity security

- Enforce administrator MFA.
- Complete passkey registration and authentication with WebAuthn.
- Add recovery codes and secure account recovery.
- Add breached-password checks and password history where required.
- Add rate limiting, adaptive lockout, and suspicious-sign-in signals.
- Add step-up authentication and recent-authentication requirements.

## Priority 4: governance maturity

- Tenant administrators and delegated administration.
- Group ownership and dynamic groups.
- Assignment start/end dates and temporary access.
- Access reviews and approval workflows.
- Entitlement packages.
- Separation-of-duties policies.
- SCIM provisioning and deprovisioning.
- Automated joiner/mover/leaver workflows.

## Priority 5: federation

- Generic OIDC provider support.
- SAML 2.0 federation.
- Provider metadata validation and health checks.
- Home-realm discovery using verified domains.
- Microsoft Entra issuer and tenant allow-list validation.
- Claim transformation and mapping rules.

## Priority 6: operations

- Structured telemetry and distributed tracing.
- Real dashboard metrics and alerting.
- Secret and certificate expiration alerts.
- Background cleanup and retention jobs.
- Exportable audit reports.
- Backup/restore drills and disaster-recovery documentation.
- CI/CD with DEV, QA, and production environments.

## Long-term vision

Auth2Demo should evolve into a secure enterprise identity platform with standards-compliant protocols, tenant-aware federation, governed application access, delegated administration, strong observability, and automated lifecycle management.

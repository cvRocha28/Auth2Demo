# Administration portal

The administration portal is organized into workspaces that align with identity-platform responsibilities.

## Overview

### Dashboard

`/Admin/Dashboard`

Operational summary for users, applications, sessions, security, and configuration.

### Platform health

`/Admin/Health`

Shows service and persistence health indicators. Do not expose connection strings, secret values, or exception internals.

## Directory

### Directory overview

`/Admin/Directory`

Cross-tenant directory summary.

### Users

`/Admin/Directory/Users` and `/Admin/Users`

Global identity discovery and user account administration. Tenant membership belongs to Directory/TenantDirectory workflows rather than being hidden inside the global user record.

### Groups

`/Admin/Directory/Groups`

Global group discovery. Group ownership remains tenant-scoped.

### Companies

`/Admin/Companies`

Company lifecycle and tenant entry point. The Users and Groups action should open the contextual Tenant Directory.

### Roles and permissions

`/Admin/Roles` and `/Admin/Permissions`

Platform administration roles and permission mappings. These are distinct from enterprise application roles.

## Applications

### App registrations

`/Admin/Clients`

OpenIddict application definitions, protocol configuration, scopes, secrets, branding, and authentication methods.

### Enterprise applications

`/Admin/EnterpriseApplications`

Allowed tenants, providers, assignment requirements, application roles, and user/group assignments.

### Scopes and claims

`/Admin/Scopes`

Scope administration and related claim behavior.

### Token explorer

`/Admin/TokenExplorer`

Development and troubleshooting utility. Production access should be tightly restricted and sensitive token values must not be persisted.

## Identity and authentication

### Identity providers

`/Admin/IdentityProviders`

Tenant-owned provider configuration and status.

### MFA

`/Admin/Mfa`

Administrative view of MFA methods and enrollment-related operations.

### Passkeys

`/Admin/Passkeys`

Administrative view of registered passkey credentials.

### Security settings

`/Admin/SecuritySettings`

Central password and lockout policy. The persisted configuration must be reflected in registration and every profile password flow.

## Monitoring and audit

- `/Admin/Sessions`
- `/Admin/Devices`
- `/Admin/AuditLogs`
- `/Admin/ApplicationAudit`
- `/Admin/ApplicationSecretsAudit`

Audit pages should support traceability without revealing credentials or token material.

## Experience

### Branding

`/Admin/Branding` and client-specific branding screens.

### Email templates

`/Admin/EmailTemplates`

Transactional template administration. Template variables should be validated and output must be encoded appropriately for its destination.

## Authorization policies

Administrative controllers use role-based policies:

- Admin;
- Client Manager;
- User Manager.

Every new controller must select the narrowest appropriate policy. UI visibility is not authorization; server-side policies remain mandatory.

## State-changing actions

All create, edit, delete, enable, disable, revoke, assignment, and membership actions should:

- use POST, PUT, PATCH, or DELETE semantics as appropriate;
- validate antiforgery tokens for browser forms;
- revalidate tenant and object ownership;
- handle concurrency and duplicate constraints;
- create an audit entry;
- return a clear success or validation message.

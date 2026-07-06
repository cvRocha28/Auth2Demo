# Administration

The Auth2Demo administration portal provides the management experience for the identity platform. It centralizes configuration for clients, scopes, permissions, identity providers, branding, users, security settings, audit data, sessions, devices, MFA, and passkeys.

## Main administration areas

- Dashboard
- Clients
- Scopes
- Permissions
- Identity Providers
- Users
- Roles
- MFA
- Passkeys
- Sessions
- Devices
- Branding
- Security Settings
- Email Templates
- Audit Logs
- Application Audit
- Application Secrets Audit
- Token Explorer
- Health

## Clients

The Clients area is used to manage OpenID Connect/OAuth applications. It supports professional client administration including:

- Client creation and editing
- Client type configuration
- Grant type configuration
- Redirect URIs
- Post logout redirect URIs
- API permissions
- Required claims
- Client secrets
- Branding configuration
- Per-client authentication methods
- Client audit visibility

## Client branding

The Client Branding page allows each application to have its own white-label authentication experience.

Configuration areas:

- General
- Colors
- Appearance
- Authentication Methods
- Advanced

The live preview reflects the selected branding, theme, colors, copy, layout options, and authentication methods.

## Authentication Methods tab

The Authentication Methods tab controls which login options are available for the selected client.

Supported options:

- Username and password
- External providers that are enabled in the IdentityProviders table

This allows administrators to create different authentication policies per client. For example, an internal admin application may allow username/password and Microsoft login, while another client may only expose a corporate provider.

## Identity Providers

The Identity Providers area manages external login providers. Enabled providers can be used by authentication pages and can appear as selectable options in client-level Authentication Methods configuration.

Provider configuration is designed to be database-driven so the login experience can be changed without hardcoding providers in the UI.

## Branding

The global Branding area defines default Auth2Demo branding. Client-specific branding can override the global settings when configured.

The default branding should remain Auth2Demo-oriented and professional. Temporary project-specific themes should not be used as the platform default.

## Auditing

The admin portal includes multiple audit screens to provide visibility into security and configuration changes.

Audit areas include:

- General audit logs
- Application audit records
- Application secret audit records

Auditing is important for client management, secret lifecycle tracking, administrator accountability, and production readiness.

## Security settings

Security settings centralize administrative configuration related to authentication behavior and security posture. This area is intended to grow as the project adds more production-grade features.

## Token Explorer

The Token Explorer area is intended to help inspect and understand token-related information during development, troubleshooting, and administration.

## Administration goals

- Provide a professional identity provider management experience
- Avoid hardcoded configuration where database-driven configuration is more appropriate
- Keep sensitive operations auditable
- Make client-level behavior explicit
- Keep UI consistent with the real runtime authentication behavior

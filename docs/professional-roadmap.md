# Professional Roadmap

This roadmap tracks the evolution of Auth2Demo into a professional OAuth 2.0 and OpenID Connect identity provider.

## Completed or in progress

### Client management

- Client administration through the UI
- Redirect URI management
- Post logout redirect URI management
- API permissions
- Required claims
- Client details page
- Client secret management
- Multiple secrets per client
- Secret rotation support
- Application audit screens
- Application secret audit screens

### Branding and white-label experience

- Global Auth2Demo branding
- Per-client branding configuration
- Professional theme presets
- Live preview
- Login page branding resolution
- Authorization page visual improvements
- Client-specific authentication method configuration

### Authentication methods

- Username and password option per client
- External provider options per client
- Providers loaded from enabled IdentityProviders records
- Live preview synchronized with selected methods
- Real authentication screen synchronized with selected methods

### Administration portal

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
- Token Explorer
- Health

### Localization

- Resource-based localization
- English and Portuguese resources
- User profile culture support
- Ongoing cleanup of hardcoded UI text

## Next milestones

### OAuth/OIDC discovery

Improve and document the `.well-known/openid-configuration` experience, including metadata validation and examples for client applications.

### Consent screen

Create a more complete consent experience similar to commercial identity providers.

Planned improvements:

- Scope grouping
- User-friendly permission descriptions
- Client trust indicators
- Remembered consent
- Admin-configurable consent policies

### Advanced scopes and claims

Expand scope and claim management.

Planned improvements:

- Custom claim mapping
- Claim rules per client
- Scope descriptions for consent
- API resource grouping
- Claim preview/testing tools

### Passkeys and WebAuthn

Complete production-ready passkey support.

Planned improvements:

- Passkey enrollment
- Passkey authentication
- Device naming
- Recovery flows
- Admin visibility
- Security event auditing

### MFA policies

Expand MFA into a full policy-driven system.

Planned improvements:

- MFA requirement per client
- MFA requirement per role
- Recovery code policies
- Enrollment enforcement
- Admin reset flows

### Advanced auditing

Improve audit coverage and reporting.

Planned improvements:

- Filtering and export
- Actor and target details
- Before/after values
- Security event severity
- Audit retention configuration

### Dashboard with real metrics

Expand the dashboard with real operational metrics.

Planned metrics:

- Login volume
- Failed login attempts
- Active clients
- Active users
- Token activity
- Provider usage
- MFA adoption
- Secret expiration warnings

### Federation improvements

Improve support for enterprise federation scenarios.

Planned improvements:

- More provider types
- Provider-specific settings
- Per-client provider restrictions
- Provider health validation
- Better callback troubleshooting

### Production hardening

Prepare Auth2Demo for stronger production use.

Planned improvements:

- Stronger security headers
- Rate limiting
- Lockout policies
- Secret storage hardening
- Admin authorization policies
- Background cleanup jobs
- Token and authorization retention policies

## Long-term vision

Auth2Demo should become a complete identity provider platform with a professional admin portal, customizable authentication experience, strong auditing, production-grade security, and a clean architecture that can support real enterprise scenarios.

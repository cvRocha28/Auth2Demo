# Tenant governance

Auth2Demo models enterprise access using companies, memberships, groups, tenant-owned providers, enterprise applications, and assignments.

## Company

`Company` is the internal tenant boundary. It owns directory groups and identity providers and can be allowed to use one or more enterprise applications.

Typical examples:

```text
Atento
Interfile
Subsidiary A
Subsidiary B
```

## User membership

`CompanyUser` represents a user's relationship with a company. A user can belong to multiple companies.

Membership data includes:

- company and user identifiers;
- enabled state;
- default-company flag;
- department;
- job title;
- audit timestamps.

Only an enabled membership may be the default membership. When the default membership is removed, another enabled membership should be promoted when one exists.

## Groups

`CompanyGroup` belongs to exactly one company. `CompanyGroupMember` links a user to a group.

Rules:

- group names are unique inside a company;
- only users with an active membership in that company should be added;
- disabled groups must not grant new access;
- cross-company membership must be rejected;
- group deletion must clean dependent application assignments explicitly and transactionally.

## Tenant-owned identity providers

`IdentityProvider.CompanyId` identifies the tenant that owns the provider configuration. A provider may represent Microsoft, Google, OIDC, SAML, or another future federation mechanism.

Tenant ownership enables independent authorities, client IDs, secrets, claim mappings, branding, and operational policies.

## Application tenant access

`ApplicationTenantAssignment` links an OpenIddict application to an allowed company. It also records whether explicit user or group assignment is mandatory.

```text
Application: Atento HR Portal
Allowed tenant: Atento, assignment required
Allowed tenant: Interfile, assignment required
```

The owner company should normally be included among the allowed tenants.

## Provider access

`ApplicationIdentityProvider` defines which configured providers may appear for an application. A provider should only be selectable when its owning company is allowed for that application and the provider is active and correctly configured.

## Application roles

`EnterpriseApplicationRole` defines roles scoped to one application, such as:

```text
Reader
User
Manager
Administrator
Approver
```

The role value is what should be emitted in the token. Role values must be unique inside one application.

## User and group assignments

`EnterpriseApplicationAssignment` represents either a direct user assignment or a group assignment.

The database check constraint enforces the shape:

```text
PrincipalType = User  -> UserId required, GroupId null
PrincipalType = Group -> GroupId required, UserId null
```

An optional `ApplicationRoleId` grants an application role through the assignment.

## Access algorithm

A recommended evaluation sequence is:

```text
1. Resolve the application by client ID.
2. Resolve the user's active company memberships.
3. Find allowed tenant assignments intersecting those memberships.
4. Validate the provider, when external authentication was used.
5. For each allowed tenant:
   a. If assignment is not required, tenant access is permitted.
   b. If assignment is required, look for an enabled direct assignment.
   c. Otherwise, look for an enabled assignment through an active group.
6. Collect enabled application roles from valid assignments.
7. Return an explicit allow or deny result and reason code.
```

Rules must live in `IEnterpriseApplicationAccessEvaluator` and its implementation, not in Razor views or controllers.

## Referential integrity

SQL Server can reject multiple cascade paths. The relationship from enterprise assignment to application role uses `DeleteBehavior.NoAction`; role deletion therefore must be handled explicitly by the application service.

This design avoids this conflicting cascade graph:

```text
Application -> Assignment
Application -> Role -> Assignment
```

## Auditing expectations

The following operations should generate audit entries:

- company creation or status change;
- membership creation, update, disable, or removal;
- group creation, member change, disable, or deletion;
- allowed-tenant changes;
- provider allow-list changes;
- assignment and role changes;
- access denials caused by governance policy.

# Directory administration

Directory administration is split into global and tenant-scoped workspaces so each page has one clear responsibility.

## Global directory

Routes under `/Admin/Directory` provide cross-tenant discovery and reporting.

### Overview

`/Admin/Directory`

Shows global counts and tenant summaries. It is the entry point for administrators who need to search across the platform.

### Users

`/Admin/Directory/Users`

Provides global user search and company filtering. It should display user identity, status, memberships, and group counts, with safe navigation to account or tenant membership management.

### Groups

`/Admin/Directory/Groups`

Provides global group search and company filtering. It should display tenant ownership, enabled state, member count, and enterprise application assignment count.

### Group details

`/Admin/Directory/Group/{id}`

Manages one group's metadata and members. Membership validation must enforce tenant boundaries.

## Tenant directory

`/Admin/TenantDirectory?companyId={companyId}` is a contextual workspace for one company. It should only show operations that belong to the selected tenant.

### Tenant overview

The overview summarizes:

- active and disabled memberships;
- groups and member totals;
- provider and application access indicators;
- links to tenant users, tenant groups, company settings, and enterprise applications.

It should not duplicate global user administration, security policy, provider editing, or enterprise assignment screens.

### Tenant users

`/Admin/TenantDirectory/Users?companyId={companyId}`

Responsibilities:

- add an existing platform user to the company;
- edit department and job title;
- enable or disable the membership;
- select the user's default company;
- remove the membership safely.

Removing a membership should transactionally remove that user's memberships in groups owned by the company and direct enterprise assignments for the same tenant. It should not delete the global user account.

### Tenant groups

`/Admin/TenantDirectory/Groups?companyId={companyId}`

Responsibilities:

- list groups owned by the company;
- search and filter groups;
- create a group;
- navigate to member management;
- enable, disable, or remove groups through the appropriate group-detail workflow.

## UX conventions

- Always display the current tenant name and status.
- Preserve `companyId` through links, redirects, filters, and form posts.
- Use explicit empty states rather than blank tables.
- Confirm destructive operations.
- Distinguish a disabled platform user from a disabled tenant membership.
- Explain the effect of removing a membership before confirmation.
- Return clear validation errors for cross-tenant operations.
- Use antiforgery tokens on all state-changing forms.

## Layer responsibilities

### Domain

Owns membership, group, and assignment concepts and invariant-friendly entity behavior.

### Application

Defines commands, queries, DTOs, and governance service contracts. It should not reference MVC view models.

### Infrastructure

Implements filtered queries, transaction boundaries, cleanup rules, relational constraints, and access evaluation.

### Web

Owns tenant-scoped routes, forms, view models, authorization, messages, and navigation.

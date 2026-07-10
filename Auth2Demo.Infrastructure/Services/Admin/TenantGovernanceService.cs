using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Auth2Demo.Infrastructure.Services.Admin;

public sealed class TenantGovernanceService : ITenantGovernanceService, IEnterpriseApplicationAccessEvaluator
{
    private readonly ApplicationDbContext _db;
    public TenantGovernanceService(ApplicationDbContext db) => _db = db;

    public async Task<DirectoryOverviewData> GetDirectoryOverviewAsync()
    {
        var users = _db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        var memberships = _db.CompanyUsers.AsNoTracking();
        var groups = _db.CompanyGroups.AsNoTracking();
        var companiesQuery = _db.Companies.AsNoTracking();

        var totalUsers = await users.CountAsync();
        var activeUsers = await users.CountAsync(x => x.Status == UserStatus.Active);
        var activeMemberships = await memberships.CountAsync(x => x.IsEnabled);
        var totalMemberships = await memberships.CountAsync();
        var totalGroups = await groups.CountAsync();
        var activeGroups = await groups.CountAsync(x => x.IsEnabled);
        var totalCompanies = await companiesQuery.CountAsync();
        var activeCompanies = await companiesQuery.CountAsync(x => x.IsEnabled);
        var applicationAssignments = await _db.EnterpriseApplicationAssignments.AsNoTracking()
            .CountAsync(x => x.IsEnabled);

        var unassignedUsers = await users.CountAsync(user =>
            !_db.CompanyUsers.Any(membership => membership.UserId == user.Id && membership.IsEnabled));

        var companies = await companiesQuery
            .OrderByDescending(x => x.IsEnabled)
            .ThenBy(x => x.DisplayName)
            .Select(x => new DirectoryCompanySummaryData
            {
                Id = x.Id,
                Name = x.DisplayName,
                DomainHint = x.DomainHint,
                Country = x.Country,
                Culture = x.Culture,
                IsActive = x.IsEnabled,
                UserCount = _db.CompanyUsers.Count(m => m.CompanyId == x.Id && m.IsEnabled),
                DisabledUserCount = _db.CompanyUsers.Count(m => m.CompanyId == x.Id && !m.IsEnabled),
                GroupCount = _db.CompanyGroups.Count(g => g.CompanyId == x.Id && g.IsEnabled),
                DisabledGroupCount = _db.CompanyGroups.Count(g => g.CompanyId == x.Id && !g.IsEnabled),
                ProviderCount = _db.IdentityProviders.Count(provider => provider.CompanyId == x.Id && provider.IsEnabled),
                ApplicationAssignmentCount = _db.EnterpriseApplicationAssignments.Count(assignment =>
                    assignment.CompanyId == x.Id && assignment.IsEnabled)
            })
            .ToListAsync();

        return new DirectoryOverviewData
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            BlockedUsers = totalUsers - activeUsers,
            ActiveMemberships = activeMemberships,
            DisabledMemberships = totalMemberships - activeMemberships,
            TotalGroups = totalGroups,
            ActiveGroups = activeGroups,
            TotalCompanies = totalCompanies,
            ActiveCompanies = activeCompanies,
            UnassignedUsers = unassignedUsers,
            ApplicationAssignments = applicationAssignments,
            Companies = companies
        };
    }

    public async Task<IReadOnlyList<DirectoryUserRowData>> SearchDirectoryUsersAsync(Guid? companyId, string? query)
    {
        var normalized = query?.Trim();
        var users = _db.Users.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(normalized))
            users = users.Where(x => x.DisplayName.Contains(normalized) || (x.Email != null && x.Email.Contains(normalized)) || (x.UserName != null && x.UserName.Contains(normalized)));
        if (companyId.HasValue)
            users = users.Where(x => _db.CompanyUsers.Any(m => m.UserId == x.Id && m.CompanyId == companyId.Value));
        var rows = await users.OrderBy(x => x.DisplayName).ThenBy(x => x.Email).Select(x => new
        {
            x.Id,
            x.DisplayName,
            Email = x.Email ?? x.UserName ?? string.Empty,
            IsBlocked = x.Status != UserStatus.Active,
            CompanyCount = _db.CompanyUsers.Count(m => m.UserId == x.Id && m.IsEnabled),
            GroupCount = _db.CompanyGroupMembers.Count(m => m.UserId == x.Id)
        }).ToListAsync();
        var ids = rows.Select(x => x.Id).ToArray();
        var companyNames = await (from m in _db.CompanyUsers.AsNoTracking()
                                  join c in _db.Companies.AsNoTracking() on m.CompanyId equals c.Id
                                  where ids.Contains(m.UserId) && m.IsEnabled
                                  select new { m.UserId, c.DisplayName }).ToListAsync();
        return rows.Select(x => new DirectoryUserRowData
        {
            Id = x.Id,
            DisplayName = x.DisplayName,
            Email = x.Email,
            IsBlocked = x.IsBlocked,
            CompanyCount = x.CompanyCount,
            GroupCount = x.GroupCount,
            Companies = string.Join(", ", companyNames.Where(c => c.UserId == x.Id).Select(c => c.DisplayName).Distinct())
        }).ToList();
    }

    public async Task<IReadOnlyList<DirectoryGroupRowData>> SearchDirectoryGroupsAsync(Guid? companyId, string? query)
    {
        var normalized = query?.Trim();
        var groups = _db.CompanyGroups.AsNoTracking().AsQueryable();
        if (companyId.HasValue) groups = groups.Where(x => x.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(normalized)) groups = groups.Where(x => x.Name.Contains(normalized) || (x.Description != null && x.Description.Contains(normalized)));
        return await (from g in groups
                      join c in _db.Companies.AsNoTracking() on g.CompanyId equals c.Id
                      orderby c.DisplayName, g.Name
                      select new DirectoryGroupRowData
                      {
                          Id = g.Id,
                          CompanyId = g.CompanyId,
                          CompanyName = c.DisplayName,
                          Name = g.Name,
                          Description = g.Description,
                          IsEnabled = g.IsEnabled,
                          MemberCount = _db.CompanyGroupMembers.Count(m => m.GroupId == g.Id),
                          ApplicationAssignmentCount = _db.EnterpriseApplicationAssignments.Count(a => a.GroupId == g.Id && a.IsEnabled)
                      }).ToListAsync();
    }

    public async Task<DirectoryGroupDetailsData?> GetGroupDetailsAsync(Guid groupId)
    {
        var groupInfo = await (from g in _db.CompanyGroups.AsNoTracking()
                           join c in _db.Companies.AsNoTracking() on g.CompanyId equals c.Id
                           where g.Id == groupId
                           select new { Group = g, CompanyName = c.DisplayName }).FirstOrDefaultAsync();
        if (groupInfo is null) return null;
        var members = await (from gm in _db.CompanyGroupMembers.AsNoTracking()
                             join u in _db.Users.AsNoTracking() on gm.UserId equals u.Id
                             join cm in _db.CompanyUsers.AsNoTracking() on new { UserId = u.Id, CompanyId = groupInfo.Group.CompanyId } equals new { cm.UserId, cm.CompanyId }
                             where gm.GroupId == groupId
                             orderby u.DisplayName
                             select new DirectoryGroupMemberData
                             {
                                 UserId = u.Id,
                                 DisplayName = u.DisplayName,
                                 Email = u.Email ?? u.UserName ?? string.Empty,
                                 Department = cm.Department,
                                 JobTitle = cm.JobTitle
                             }).ToListAsync();
        var memberIds = members.Select(x => x.UserId).ToArray();
        var available = await (from cm in _db.CompanyUsers.AsNoTracking()
                               join u in _db.Users.AsNoTracking() on cm.UserId equals u.Id
                               where cm.CompanyId == groupInfo.Group.CompanyId && cm.IsEnabled && !memberIds.Contains(u.Id)
                               orderby u.DisplayName
                               select new DirectoryUserOptionData { Id = u.Id, DisplayName = u.DisplayName, Email = u.Email ?? u.UserName ?? string.Empty }).ToListAsync();
        return new DirectoryGroupDetailsData
        {
            Id = groupInfo.Group.Id,
            CompanyId = groupInfo.Group.CompanyId,
            CompanyName = groupInfo.CompanyName,
            Name = groupInfo.Group.Name,
            Description = groupInfo.Group.Description,
            IsEnabled = groupInfo.Group.IsEnabled,
            Members = members,
            AvailableUsers = available
        };
    }

    public async Task UpdateGroupAsync(Guid groupId, string name, string? description, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Group name is required.");
        var group = await _db.CompanyGroups.FirstOrDefaultAsync(x => x.Id == groupId) ?? throw new InvalidOperationException("Group not found.");
        var normalized = name.Trim();
        if (await _db.CompanyGroups.AnyAsync(x => x.CompanyId == group.CompanyId && x.Id != groupId && x.Name == normalized))
            throw new InvalidOperationException("A group with this name already exists in the tenant.");
        group.Update(normalized, description, isEnabled);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var group = await _db.CompanyGroups.FirstOrDefaultAsync(x => x.Id == groupId);
        if (group is null) return;
        var assignments = await _db.EnterpriseApplicationAssignments.Where(x => x.GroupId == groupId).ToListAsync();
        var members = await _db.CompanyGroupMembers.Where(x => x.GroupId == groupId).ToListAsync();
        _db.EnterpriseApplicationAssignments.RemoveRange(assignments);
        _db.CompanyGroupMembers.RemoveRange(members);
        _db.CompanyGroups.Remove(group);
        await _db.SaveChangesAsync();
    }

    public async Task<CompanyDirectoryData?> GetCompanyDirectoryAsync(Guid companyId)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == companyId);
        if (company is null) return null;
        var users = await (from membership in _db.CompanyUsers.AsNoTracking()
                           join user in _db.Users.AsNoTracking() on membership.UserId equals user.Id
                           where membership.CompanyId == companyId
                           orderby user.DisplayName, user.Email
                           select new CompanyUserData
                           {
                               MembershipId = membership.Id, UserId = user.Id, DisplayName = user.DisplayName,
                               Email = user.Email ?? user.UserName ?? string.Empty, Department = membership.Department,
                               JobTitle = membership.JobTitle, IsEnabled = membership.IsEnabled, IsDefault = membership.IsDefault
                           }).ToListAsync();
        var groups = await _db.CompanyGroups.AsNoTracking().Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name).Select(x => new CompanyGroupData
            {
                Id=x.Id, Name=x.Name, Description=x.Description, IsEnabled=x.IsEnabled,
                MemberCount=_db.CompanyGroupMembers.Count(m => m.GroupId == x.Id)
            }).ToListAsync();
        var memberIds = users.Select(x => x.UserId).ToArray();
        var availableUsers = await _db.Users.AsNoTracking().Where(x => !x.IsDeleted && !memberIds.Contains(x.Id))
            .OrderBy(x => x.DisplayName).Select(x => new DirectoryUserOptionData
            { Id=x.Id, DisplayName=x.DisplayName, Email=x.Email ?? x.UserName ?? string.Empty }).ToListAsync();
        var groupIds = groups.Select(x => x.Id).ToArray();
        var applicationAssignmentCount = await _db.EnterpriseApplicationAssignments.AsNoTracking()
            .CountAsync(x => x.CompanyId == companyId && x.IsEnabled);

        return new CompanyDirectoryData
        {
            CompanyId = company.Id,
            CompanyName = company.DisplayName,
            Users = users,
            Groups = groups,
            AvailableUsers = availableUsers,
            ActiveUserCount = users.Count(x => x.IsEnabled),
            DisabledUserCount = users.Count(x => !x.IsEnabled),
            ActiveGroupCount = groups.Count(x => x.IsEnabled),
            ApplicationAssignmentCount = applicationAssignmentCount
        };
    }

    public async Task AddUserToCompanyAsync(Guid companyId, Guid userId, string? department, string? jobTitle, bool isDefault)
    {
        department = NormalizeOrganizationValue(department, 160, "Department");
        jobTitle = NormalizeOrganizationValue(jobTitle, 160, "Job title");

        var companyExists = await _db.Companies.AnyAsync(x => x.Id == companyId && x.IsEnabled);
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted);
        if (!companyExists || !userExists)
            throw new InvalidOperationException("The tenant or user was not found, or is not active.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var membership = await _db.CompanyUsers.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId);
        var hasAnyMembership = await _db.CompanyUsers.AnyAsync(x => x.UserId == userId && x.IsEnabled);
        var shouldBeDefault = isDefault || !hasAnyMembership;

        if (shouldBeDefault)
            await _db.CompanyUsers.Where(x => x.UserId == userId && x.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false));

        if (membership is null)
        {
            membership = new CompanyUser(companyId, userId, shouldBeDefault);
            _db.CompanyUsers.Add(membership);
        }

        membership.Update(true, shouldBeDefault, department, jobTitle);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task UpdateCompanyUserAsync(Guid companyId, Guid membershipId, bool isEnabled, bool isDefault, string? department, string? jobTitle)
    {
        department = NormalizeOrganizationValue(department, 160, "Department");
        jobTitle = NormalizeOrganizationValue(jobTitle, 160, "Job title");

        var membership = await _db.CompanyUsers.FirstOrDefaultAsync(x => x.Id == membershipId && x.CompanyId == companyId)
            ?? throw new InvalidOperationException("Tenant membership was not found.");

        if (!isEnabled && isDefault)
            throw new InvalidOperationException("A disabled membership cannot be the default tenant.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        if (isDefault)
            await _db.CompanyUsers.Where(x => x.UserId == membership.UserId && x.Id != membership.Id && x.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false));

        membership.Update(isEnabled, isDefault, department, jobTitle);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RemoveUserFromCompanyAsync(Guid companyId, Guid membershipId)
    {
        var membership = await _db.CompanyUsers.FirstOrDefaultAsync(x => x.Id == membershipId && x.CompanyId == companyId);
        if (membership is null) return;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var tenantGroupIds = await _db.CompanyGroups.Where(x => x.CompanyId == companyId).Select(x => x.Id).ToListAsync();
        var groupMemberships = await _db.CompanyGroupMembers
            .Where(x => x.UserId == membership.UserId && tenantGroupIds.Contains(x.GroupId)).ToListAsync();
        var directAssignments = await _db.EnterpriseApplicationAssignments
            .Where(x => x.CompanyId == companyId && x.UserId == membership.UserId).ToListAsync();

        _db.CompanyGroupMembers.RemoveRange(groupMemberships);
        _db.EnterpriseApplicationAssignments.RemoveRange(directAssignments);
        _db.CompanyUsers.Remove(membership);
        await _db.SaveChangesAsync();

        if (membership.IsDefault)
        {
            var replacement = await _db.CompanyUsers
                .Where(x => x.UserId == membership.UserId && x.IsEnabled)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            if (replacement is not null)
            {
                replacement.Update(true, true, replacement.Department, replacement.JobTitle);
                await _db.SaveChangesAsync();
            }
        }

        await transaction.CommitAsync();
    }

    private static string? NormalizeOrganizationValue(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new InvalidOperationException($"{fieldName} cannot exceed {maxLength} characters.");
        return normalized;
    }

    public async Task<Guid> CreateGroupAsync(Guid companyId,string name,string? description)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Group name is required.");
        if(await _db.CompanyGroups.AnyAsync(x=>x.CompanyId==companyId&&x.Name==name.Trim())) throw new InvalidOperationException("A group with this name already exists.");
        var group=new CompanyGroup(companyId,name,description); _db.CompanyGroups.Add(group); await _db.SaveChangesAsync(); return group.Id;
    }
    public async Task AddGroupMemberAsync(Guid groupId,Guid userId)
    {
        var group=await _db.CompanyGroups.FindAsync(groupId) ?? throw new InvalidOperationException("Group not found.");
        if(!await _db.CompanyUsers.AnyAsync(x=>x.CompanyId==group.CompanyId&&x.UserId==userId&&x.IsEnabled)) throw new InvalidOperationException("The user must be an active member of the tenant.");
        if(!await _db.CompanyGroupMembers.AnyAsync(x=>x.GroupId==groupId&&x.UserId==userId)){_db.CompanyGroupMembers.Add(new CompanyGroupMember(groupId,userId));await _db.SaveChangesAsync();}
    }
    public async Task RemoveGroupMemberAsync(Guid groupId,Guid userId)
    {
        var item=await _db.CompanyGroupMembers.FirstOrDefaultAsync(x=>x.GroupId==groupId&&x.UserId==userId); if(item is null)return;
        _db.CompanyGroupMembers.Remove(item); await _db.SaveChangesAsync();
    }

    public async Task<EnterpriseAssignmentPageData?> GetAssignmentsAsync(Guid applicationId)
    {
        var app=await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>().AsNoTracking().Where(x=>x.Id==applicationId)
            .Select(x=>new{x.Id,x.ClientId,x.DisplayName}).FirstOrDefaultAsync(); if(app is null)return null;
        var roles=await _db.EnterpriseApplicationRoles.AsNoTracking().Where(x=>x.ApplicationId==applicationId).OrderBy(x=>x.Name)
            .Select(x=>new EnterpriseApplicationRoleData{Id=x.Id,Name=x.Name,Value=x.Value,Description=x.Description,IsEnabled=x.IsEnabled}).ToListAsync();
        var assignmentsRaw=await _db.EnterpriseApplicationAssignments.AsNoTracking().Where(x=>x.ApplicationId==applicationId)
            .Include(x=>x.Group).Include(x=>x.ApplicationRole).ToListAsync();
        var userIds=assignmentsRaw.Where(x=>x.UserId.HasValue).Select(x=>x.UserId!.Value).Distinct().ToArray();
        var userNames=await _db.Users.AsNoTracking().Where(x=>userIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>string.IsNullOrWhiteSpace(x.DisplayName)?(x.Email??x.UserName??x.Id.ToString()):x.DisplayName);
        var companies=await _db.Companies.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.DisplayName);
        var assignments=assignmentsRaw.Select(x=>new EnterpriseAssignmentRowData{Id=x.Id,CompanyId=x.CompanyId,CompanyName=companies.GetValueOrDefault(x.CompanyId,"Unknown"),PrincipalType=x.PrincipalType,PrincipalName=x.PrincipalType==EnterpriseAssignmentType.Group?(x.Group?.Name??"Unknown group"):userNames.GetValueOrDefault(x.UserId??Guid.Empty,"Unknown user"),RoleName=x.ApplicationRole?.Name,IsEnabled=x.IsEnabled}).ToList();
        var allowedCompanies=await _db.ApplicationTenantAssignments.AsNoTracking().Where(x=>x.ApplicationId==applicationId&&x.IsEnabled).Select(x=>x.CompanyId).ToListAsync();
        var userPrincipals=await (from m in _db.CompanyUsers.AsNoTracking() join u in _db.Users.AsNoTracking() on m.UserId equals u.Id join c in _db.Companies.AsNoTracking() on m.CompanyId equals c.Id where allowedCompanies.Contains(m.CompanyId)&&m.IsEnabled select new EnterpriseAssignablePrincipalData{Id=u.Id,CompanyId=c.Id,CompanyName=c.DisplayName,Name=u.DisplayName+" · "+(u.Email??u.UserName),Type=EnterpriseAssignmentType.User}).ToListAsync();
        var groupPrincipals=await (from g in _db.CompanyGroups.AsNoTracking() join c in _db.Companies.AsNoTracking() on g.CompanyId equals c.Id where allowedCompanies.Contains(g.CompanyId)&&g.IsEnabled select new EnterpriseAssignablePrincipalData{Id=g.Id,CompanyId=c.Id,CompanyName=c.DisplayName,Name=g.Name,Type=EnterpriseAssignmentType.Group}).ToListAsync();
        return new EnterpriseAssignmentPageData{ApplicationId=app.Id,ApplicationName=app.DisplayName??app.ClientId??"Application",ClientId=app.ClientId??string.Empty,Assignments=assignments,Roles=roles,Principals=userPrincipals.Concat(groupPrincipals).OrderBy(x=>x.CompanyName).ThenBy(x=>x.Type).ThenBy(x=>x.Name).ToList()};
    }
    public async Task AssignAsync(Guid applicationId,Guid companyId,EnterpriseAssignmentType type,Guid principalId,Guid? roleId)
    {
        if(!await _db.ApplicationTenantAssignments.AnyAsync(x=>x.ApplicationId==applicationId&&x.CompanyId==companyId&&x.IsEnabled))
            throw new InvalidOperationException("The tenant is not enabled for this application.");

        if (roleId.HasValue)
        {
            var roleIsValid = await _db.EnterpriseApplicationRoles.AnyAsync(x =>
                x.Id == roleId.Value &&
                x.ApplicationId == applicationId &&
                x.IsEnabled);

            if (!roleIsValid)
                throw new InvalidOperationException("The selected role is not enabled for this enterprise application.");
        }

        EnterpriseApplicationAssignment assignment;
        if(type==EnterpriseAssignmentType.User){if(!await _db.CompanyUsers.AnyAsync(x=>x.CompanyId==companyId&&x.UserId==principalId&&x.IsEnabled))throw new InvalidOperationException("The user is not an active tenant member.");assignment=EnterpriseApplicationAssignment.ForUser(applicationId,companyId,principalId,roleId);}
        else {if(!await _db.CompanyGroups.AnyAsync(x=>x.Id==principalId&&x.CompanyId==companyId&&x.IsEnabled))throw new InvalidOperationException("The group is not valid for this tenant.");assignment=EnterpriseApplicationAssignment.ForGroup(applicationId,companyId,principalId,roleId);}
        var exists=await _db.EnterpriseApplicationAssignments.AnyAsync(x=>x.ApplicationId==applicationId&&x.CompanyId==companyId&&x.PrincipalType==type&&(type==EnterpriseAssignmentType.User?x.UserId==principalId:x.GroupId==principalId));
        if(!exists){_db.EnterpriseApplicationAssignments.Add(assignment);await _db.SaveChangesAsync();}
    }
    public async Task RemoveAssignmentAsync(Guid assignmentId){var item=await _db.EnterpriseApplicationAssignments.FindAsync(assignmentId);if(item is null)return;_db.Remove(item);await _db.SaveChangesAsync();}
    public async Task<Guid> SaveRoleAsync(Guid applicationId,Guid? roleId,string name,string value,string? description,bool enabled)
    {
        if(string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(value))throw new InvalidOperationException("Role name and value are required.");
        var role=roleId.HasValue?await _db.EnterpriseApplicationRoles.FirstOrDefaultAsync(x=>x.Id==roleId&&x.ApplicationId==applicationId):null;
        if(role is null){role=new EnterpriseApplicationRole(applicationId,name,value,description);_db.Add(role);}else role.Update(name,value,description,enabled);
        await _db.SaveChangesAsync();return role.Id;
    }

    public async Task<EnterpriseAccessResult> EvaluateAsync(Guid userId,Guid applicationId,CancellationToken cancellationToken=default)
    {
        var applicationState = await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AsNoTracking()
            .Where(x => x.Id == applicationId)
            .Select(x => new
            {
                IsEnabled = EF.Property<bool>(x, "IsEnabled"),
                IsDeleted = EF.Property<bool>(x, "IsDeleted")
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (applicationState is null || applicationState.IsDeleted)
            return new(false, "This application is no longer available.", null, Array.Empty<string>());

        if (!applicationState.IsEnabled)
            return new(false, "This enterprise application is disabled. Contact an administrator to enable it.", null, Array.Empty<string>());

        var memberships=await _db.CompanyUsers.AsNoTracking().Where(x=>x.UserId==userId&&x.IsEnabled).Select(x=>x.CompanyId).ToListAsync(cancellationToken);
        if(memberships.Count==0){var legacy=await _db.Users.AsNoTracking().Where(x=>x.Id==userId).Select(x=>x.CompanyId).FirstOrDefaultAsync(cancellationToken);if(legacy.HasValue)memberships.Add(legacy.Value);}
        var hasTenantConfiguration = await _db.ApplicationTenantAssignments.AsNoTracking()
            .AnyAsync(x => x.ApplicationId == applicationId && x.IsEnabled, cancellationToken);
        if (!hasTenantConfiguration)
            return new(true, null, memberships.FirstOrDefault(), Array.Empty<string>());

        // Tenant allow-listing is always enforced. RequireUserAssignment only adds a second
        // principal-level check inside an already allowed tenant; disabling it must never
        // turn the application into a cross-tenant/public application.
        var tenantRules = await _db.ApplicationTenantAssignments.AsNoTracking()
            .Where(x => x.ApplicationId == applicationId && x.IsEnabled && memberships.Contains(x.CompanyId))
            .OrderBy(x => x.CompanyId)
            .ToListAsync(cancellationToken);
        if (tenantRules.Count == 0)
            return new(false, "Your tenant is not authorized for this application.", null, Array.Empty<string>());
        var groups=await (from gm in _db.CompanyGroupMembers.AsNoTracking() join g in _db.CompanyGroups.AsNoTracking() on gm.GroupId equals g.Id where gm.UserId==userId&&g.IsEnabled select gm.GroupId).ToListAsync(cancellationToken);
        var assignments=await _db.EnterpriseApplicationAssignments.AsNoTracking().Include(x=>x.ApplicationRole).Where(x=>x.ApplicationId==applicationId&&x.IsEnabled&&memberships.Contains(x.CompanyId)&&((x.UserId==userId)||(x.GroupId!=null&&groups.Contains(x.GroupId.Value)))).ToListAsync(cancellationToken);
        var required=tenantRules.Where(x=>x.RequireUserAssignment).Select(x=>x.CompanyId).ToHashSet();
        if(required.Count>0&&!assignments.Any(x=>required.Contains(x.CompanyId)))return new(false,"Your account is valid, but it has not been assigned to this application.",required.First(),Array.Empty<string>());
        var roles=assignments.Where(x=>x.ApplicationRole?.IsEnabled==true).Select(x=>x.ApplicationRole!.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Prefer a tenant that does not require assignment when more than one allowed
        // membership is available. This keeps the effective tenant deterministic.
        var effectiveTenant = tenantRules
            .OrderBy(x => x.RequireUserAssignment)
            .ThenBy(x => x.CompanyId)
            .First().CompanyId;
        return new(true, null, effectiveTenant, roles);
    }
}

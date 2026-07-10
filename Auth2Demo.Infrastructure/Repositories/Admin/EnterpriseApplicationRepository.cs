using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Auth2Demo.Infrastructure.Repositories.Admin;

public sealed class EnterpriseApplicationRepository : IEnterpriseApplicationRepository
{
    private readonly ApplicationDbContext _db;

    public EnterpriseApplicationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseApplicationListItemData>> ListAsync()
    {
        return await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.ClientId)
            .Select(x => new EnterpriseApplicationListItemData
            {
                ApplicationId = x.Id,
                ClientId = x.ClientId ?? string.Empty,
                DisplayName = x.DisplayName ?? x.ClientId ?? string.Empty,
                OwnerCompanyName = _db.Companies
                    .Where(c => c.Id == EF.Property<Guid?>(x, "CompanyId"))
                    .Select(c => c.DisplayName)
                    .FirstOrDefault(),
                AllowedCompanyCount = _db.ApplicationTenantAssignments.Count(a => a.ApplicationId == x.Id && a.IsEnabled),
                AllowedProviderCount = _db.ApplicationIdentityProviders.Count(p => p.ApplicationId == x.Id && p.IsEnabled),
                UserAssignmentRequiredCount = _db.ApplicationTenantAssignments.Count(a => a.ApplicationId == x.Id && a.IsEnabled && a.RequireUserAssignment),
                IsEnabled = EF.Property<bool>(x, "IsEnabled")
            })
            .ToListAsync();
    }

    public async Task<EnterpriseApplicationEditData?> GetForEditAsync(Guid applicationId)
    {
        var app = await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AsNoTracking()
            .Where(x => x.Id == applicationId)
            .Select(x => new
            {
                x.Id,
                x.ClientId,
                x.DisplayName,
                IsEnabled = EF.Property<bool>(x, "IsEnabled"),
                OwnerCompanyId = EF.Property<Guid?>(x, "CompanyId")
            })
            .FirstOrDefaultAsync();

        if (app is null) return null;

        var assignments = await _db.ApplicationTenantAssignments
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .ToDictionaryAsync(x => x.CompanyId);

        var allowedProviderIds = await _db.ApplicationIdentityProviders
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId && x.IsEnabled)
            .Select(x => x.IdentityProviderId)
            .ToListAsync();

        var companies = await _db.Companies
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.DisplayName)
            .Select(x => new
            {
                x.Id,
                x.DisplayName,
                x.DomainHint,
                x.Country,
                x.Culture,
                x.IsDefault,
                ProviderCount = _db.IdentityProviders.Count(p => p.CompanyId == x.Id && p.IsEnabled)
            })
            .ToListAsync();

        var tenantAccess = companies.Select(company =>
        {
            assignments.TryGetValue(company.Id, out var assignment);
            return new EnterpriseTenantAccessData
            {
                CompanyId = company.Id,
                CompanyName = company.DisplayName,
                DomainHint = company.DomainHint,
                Country = company.Country,
                Culture = company.Culture,
                IsDefault = company.IsDefault,
                ProviderCount = company.ProviderCount,
                IsEnabled = assignment?.IsEnabled == true,
                RequireUserAssignment = assignment?.RequireUserAssignment == true,
                Notes = assignment?.Notes
            };
        }).ToList();

        var providers = await _db.IdentityProviders
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Company == null ? "" : x.Company.DisplayName)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new IdentityProviderListItemData
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company == null ? null : x.Company.DisplayName,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Scheme = x.Scheme,
                Kind = x.Kind,
                IsEnabled = x.IsEnabled,
                IsSystemProvider = x.IsSystemProvider,
                SortOrder = x.SortOrder,
                HasClientId = x.ClientId != null && x.ClientId != string.Empty,
                HasClientSecret = x.ClientSecret != null && x.ClientSecret != string.Empty
            })
            .ToListAsync();

        return new EnterpriseApplicationEditData
        {
            ApplicationId = app.Id,
            ClientId = app.ClientId ?? string.Empty,
            DisplayName = app.DisplayName ?? app.ClientId ?? string.Empty,
            IsEnabled = app.IsEnabled,
            OwnerCompanyId = app.OwnerCompanyId,
            TenantAccess = tenantAccess,
            Providers = providers,
            AllowedProviderIds = allowedProviderIds.ToHashSet()
        };
    }

    public async Task SaveAsync(SaveEnterpriseApplicationData model)
    {
        var applicationExists = await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AnyAsync(x => x.Id == model.ApplicationId);
        if (!applicationExists)
        {
            throw new InvalidOperationException("The enterprise application no longer exists.");
        }

        var validCompanies = await _db.Companies
            .Where(x => x.IsEnabled)
            .Select(x => x.Id)
            .ToHashSetAsync();

        if (model.OwnerCompanyId.HasValue && !validCompanies.Contains(model.OwnerCompanyId.Value))
        {
            throw new InvalidOperationException("Select a valid owner company.");
        }

        // Each company is posted once by the indexed form. Grouping defensively keeps the
        // operation idempotent even if a duplicated field is submitted by a stale browser tab.
        var requestedAccess = (model.TenantAccess ?? [])
            .Where(x => x.CompanyId != Guid.Empty && validCompanies.Contains(x.CompanyId))
            .GroupBy(x => x.CompanyId)
            .ToDictionary(x => x.Key, x => x.Last());

        var enabledAccess = requestedAccess
            .Where(x => x.Value.IsEnabled)
            .ToDictionary(x => x.Key, x => x.Value);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        await _db.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .Where(x => x.Id == model.ApplicationId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => EF.Property<Guid?>(x, "CompanyId"), model.OwnerCompanyId)
                .SetProperty(x => EF.Property<bool>(x, "IsEnabled"), model.IsEnabled)
                .SetProperty(x => EF.Property<DateTimeOffset?>(x, "UpdatedAt"), DateTimeOffset.UtcNow));

        var existingTenantAssignments = await _db.ApplicationTenantAssignments
            .Where(x => x.ApplicationId == model.ApplicationId)
            .ToListAsync();

        var enabledCompanyIds = enabledAccess.Keys.ToHashSet();
        var removedCompanyIds = existingTenantAssignments
            .Where(x => !enabledCompanyIds.Contains(x.CompanyId))
            .Select(x => x.CompanyId)
            .ToHashSet();

        if (removedCompanyIds.Count > 0)
        {
            // Access assignments have no meaning after the tenant is removed from the
            // Enterprise Application. Removing them also prevents orphaned effective access.
            var principalAssignments = await _db.EnterpriseApplicationAssignments
                .Where(x => x.ApplicationId == model.ApplicationId && removedCompanyIds.Contains(x.CompanyId))
                .ToListAsync();
            _db.EnterpriseApplicationAssignments.RemoveRange(principalAssignments);

            var tenantAssignmentsToRemove = existingTenantAssignments
                .Where(x => removedCompanyIds.Contains(x.CompanyId))
                .ToList();
            _db.ApplicationTenantAssignments.RemoveRange(tenantAssignmentsToRemove);
        }

        var existingByCompany = existingTenantAssignments
            .Where(x => !removedCompanyIds.Contains(x.CompanyId))
            .ToDictionary(x => x.CompanyId);

        foreach (var (companyId, access) in enabledAccess)
        {
            if (existingByCompany.TryGetValue(companyId, out var existing))
            {
                existing.Update(true, access.RequireUserAssignment, NormalizeNotes(access.Notes));
                continue;
            }

            var assignment = new ApplicationTenantAssignment(model.ApplicationId, companyId);
            assignment.Update(true, access.RequireUserAssignment, NormalizeNotes(access.Notes));
            _db.ApplicationTenantAssignments.Add(assignment);
        }

        var validProviders = await _db.IdentityProviders
            .Where(x => x.IsEnabled && (x.CompanyId == null || enabledCompanyIds.Contains(x.CompanyId.Value)))
            .Select(x => x.Id)
            .ToHashSetAsync();

        var wantedProviders = (model.AllowedProviderIds ?? [])
            .Where(validProviders.Contains)
            .Distinct()
            .ToHashSet();

        var existingProviders = await _db.ApplicationIdentityProviders
            .Where(x => x.ApplicationId == model.ApplicationId)
            .ToListAsync();

        foreach (var entry in existingProviders)
        {
            entry.SetEnabled(wantedProviders.Contains(entry.IdentityProviderId));
            wantedProviders.Remove(entry.IdentityProviderId);
        }

        foreach (var providerId in wantedProviders)
        {
            _db.ApplicationIdentityProviders.Add(new ApplicationIdentityProvider(model.ApplicationId, providerId));
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static string? NormalizeNotes(string? notes)
    {
        var value = notes?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value.Length <= 500 ? value : value[..500];
    }
}

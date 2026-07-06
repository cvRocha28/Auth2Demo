using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth2Demo.Infrastructure.Repositories.Admin;

public interface IAdminOpenIddictMetricsRepository
{
    Task<long> CountApplicationsAsync();
    Task<long> CountScopesAsync();
}

public interface IAdminRoleRepository
{
    Task<IReadOnlyList<ApplicationRole>> ListAsync();
    Task<IReadOnlyList<AdminPermissionRoleData>> ListPermissionRolesAsync();
    Task<bool> ExistsAsync(string name);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(ApplicationRole role);
    Task<ApplicationRole?> FindByIdAsync(Guid id);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> DeleteAsync(ApplicationRole role);
    Task<IReadOnlyList<string>> ListNamesAsync();
}

public interface IAdminUserRepository
{
    Task<int> CountAsync();
    Task<int> CountCreatedFromAsync(DateTimeOffset from);
    Task<IReadOnlyList<ApplicationUser>> SearchAsync(string? query, int take);
    Task<IReadOnlyList<ApplicationUser>> ListForMfaAsync();
    Task<ApplicationUser?> FindByIdAsync(Guid id);
    Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user);
    Task<IReadOnlyList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    Task<int> CountRecoveryCodesAsync(ApplicationUser user);
    Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user);
    Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(ApplicationUser user, string password);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> UpdateAsync(ApplicationUser user);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> DeleteAsync(ApplicationUser user);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roles);
    Task<(bool Success, IEnumerable<IdentityError> Errors)> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles);
}

public sealed class AdminOpenIddictMetricsRepository : IAdminOpenIddictMetricsRepository
{
    private readonly IOpenIddictApplicationManager _applications;
    private readonly IOpenIddictScopeManager _scopes;

    public AdminOpenIddictMetricsRepository(
        IOpenIddictApplicationManager applications,
        IOpenIddictScopeManager scopes)
    {
        _applications = applications;
        _scopes = scopes;
    }

    public async Task<long> CountApplicationsAsync()
    {
        return await _applications.CountAsync();
    }

    public async Task<long> CountScopesAsync()
    {
        return await _scopes.CountAsync();
    }
}

public sealed class AdminRoleRepository : IAdminRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdminRoleRepository(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<ApplicationRole>> ListAsync()
    {
        return await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminPermissionRoleData>> ListPermissionRolesAsync()
    {
        return await _roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new AdminPermissionRoleData(role.Id, role.Name))
            .ToListAsync();
    }

    public Task<bool> ExistsAsync(string name)
    {
        return _roleManager.RoleExistsAsync(name);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(ApplicationRole role)
    {
        var result = await _roleManager.CreateAsync(role);
        return (result.Succeeded, result.Errors);
    }

    public Task<ApplicationRole?> FindByIdAsync(Guid id)
    {
        return _roleManager.FindByIdAsync(id.ToString());
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> DeleteAsync(ApplicationRole role)
    {
        var result = await _roleManager.DeleteAsync(role);
        return (result.Succeeded, result.Errors);
    }

    public async Task<IReadOnlyList<string>> ListNamesAsync()
    {
        return await _roleManager.Roles
            .Where(role => role.Name != null)
            .Select(role => role.Name!)
            .ToListAsync();
    }
}

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<int> CountAsync()
    {
        return _userManager.Users.CountAsync();
    }

    public Task<int> CountCreatedFromAsync(DateTimeOffset from)
    {
        return _userManager.Users.CountAsync(user => user.CreatedAt >= from);
    }

    public async Task<IReadOnlyList<ApplicationUser>> SearchAsync(string? query, int take)
    {
        var users = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            users = users.Where(user =>
                user.Email!.Contains(query) ||
                user.DisplayName.Contains(query));
        }

        return await users
            .OrderByDescending(user => user.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ApplicationUser>> ListForMfaAsync()
    {
        return await _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .ToListAsync();
    }

    public Task<ApplicationUser?> FindByIdAsync(Guid id)
    {
        return _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user)
    {
        return (await _userManager.GetRolesAsync(user)).ToArray();
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        return (await _userManager.GetUsersInRoleAsync(roleName)).ToArray();
    }

    public Task<int> CountRecoveryCodesAsync(ApplicationUser user)
    {
        return _userManager.CountRecoveryCodesAsync(user);
    }

    public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user)
    {
        return _userManager.GetTwoFactorEnabledAsync(user);
    }

    public Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user)
    {
        return _userManager.GetAuthenticatorKeyAsync(user);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> UpdateAsync(ApplicationUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Errors);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> DeleteAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);
        return (result.Succeeded, result.Errors);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var result = await _userManager.AddToRolesAsync(user, roles);
        return (result.Succeeded, result.Errors);
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var result = await _userManager.RemoveFromRolesAsync(user, roles);
        return (result.Succeeded, result.Errors);
    }
}

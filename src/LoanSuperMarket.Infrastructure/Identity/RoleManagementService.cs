using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Infrastructure service for managing custom roles and their permissions.
/// </summary>
public sealed class RoleManagementService : IRoleManagementService
{
    private readonly RoleManager<CustomRole> _roleManager;
    private readonly AuthIdentityDbContext _dbContext;

    public RoleManagementService(
        RoleManager<CustomRole> roleManager,
        AuthIdentityDbContext dbContext)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<(bool Succeeded, string RoleId, IEnumerable<string> Errors)> CreateRoleAsync(
        string name,
        string description,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var role = new CustomRole
        {
            Name = name,
            Description = description,
            IsSystemRole = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return (false, string.Empty, result.Errors.Select(e => e.Description));
        }

        return (true, role.Id, Enumerable.Empty<string>());
    }

    public async Task<CustomRole?> GetRoleByIdAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    public async Task<CustomRole?> GetRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return await _roleManager.FindByNameAsync(roleName);
    }

    public async Task<bool> UpdateRoleDescriptionAsync(
        string roleId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            return false;
        }

        role.Description = description;
        var result = await _roleManager.UpdateAsync(role);

        return result.Succeeded;
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteRoleAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            return (false, new[] { "Role not found." });
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        return (true, Enumerable.Empty<string>());
    }

    public async Task ReplacePermissionsAsync(
        string roleId,
        IReadOnlyList<PermissionDto> permissions,
        string grantedBy,
        CancellationToken cancellationToken = default)
    {
        // Remove existing permissions for this role
        var existingPermissions = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.RolePermissions.RemoveRange(existingPermissions);

        // Add new permissions
        var newPermissions = permissions.Select(p => new RolePermission
        {
            RoleId = roleId,
            Module = Enum.Parse<PermissionModule>(p.Module, ignoreCase: true),
            Action = Enum.Parse<PermissionAction>(p.Action, ignoreCase: true),
            GrantedAtUtc = DateTime.UtcNow,
            GrantedBy = grantedBy
        });

        await _dbContext.RolePermissions.AddRangeAsync(newPermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(
        string roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new PermissionDto
            {
                Module = rp.Module.ToString(),
                Action = rp.Action.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}

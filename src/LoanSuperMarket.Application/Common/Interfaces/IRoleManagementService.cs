using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Domain.Entities.Identity;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for managing custom roles and their permissions.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Creates a new custom role with the specified name and description.
    /// </summary>
    Task<(bool Succeeded, string RoleId, IEnumerable<string> Errors)> CreateRoleAsync(
        string name,
        string description,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom role by its identifier.
    /// </summary>
    Task<CustomRole?> GetRoleByIdAsync(
        string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a custom role by its name.
    /// </summary>
    Task<CustomRole?> GetRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the description of an existing role.
    /// </summary>
    Task<bool> UpdateRoleDescriptionAsync(
        string roleId,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role by its identifier.
    /// </summary>
    Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteRoleAsync(
        string roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all permissions for a role with the specified set.
    /// </summary>
    Task ReplacePermissionsAsync(
        string roleId,
        IReadOnlyList<PermissionDto> permissions,
        string grantedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions assigned to a role.
    /// </summary>
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(
        string roleId,
        CancellationToken cancellationToken = default);
}

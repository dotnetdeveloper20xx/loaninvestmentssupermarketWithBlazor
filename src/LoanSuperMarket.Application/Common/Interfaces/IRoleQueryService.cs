using LoanSuperMarket.Application.Features.Roles.Models;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Query service for role-related read operations that require direct database access.
/// </summary>
public interface IRoleQueryService
{
    /// <summary>
    /// Gets all roles with their user counts.
    /// </summary>
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions assigned to a specific role.
    /// </summary>
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(
        string roleId,
        CancellationToken cancellationToken = default);
}

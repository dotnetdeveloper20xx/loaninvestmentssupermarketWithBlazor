namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Resolves effective permissions for a user by computing the union of all
/// permissions from their assigned roles (predefined + custom).
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Gets the effective permissions for a user as the union of all permissions
    /// from their assigned roles. Returns permissions in "Module.Action" format.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of permission strings in "Module.Action" format (e.g., "UserManagement.View").</returns>
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(string userId);

    /// <summary>
    /// Simulates the effective permissions for a user with additional hypothetical roles.
    /// Used by the admin permission testing tool to preview what a user would be able to access.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="additionalRoles">Additional role names to include in the simulation.</param>
    /// <returns>A list of permission strings in "Module.Action" format.</returns>
    Task<IReadOnlyList<string>> SimulatePermissionsAsync(string userId, IReadOnlyList<string> additionalRoles);
}

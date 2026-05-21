using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current authenticated user's identity and claims.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The current user's unique identifier, or null if not authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The current user's email address, or null if not authenticated.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Whether the current request is from an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user holds the specified role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the current user has the specified permission (module + action combination).
    /// </summary>
    bool HasPermission(PermissionModule module, PermissionAction action);
}

using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for managing user identity operations (registration, credentials, roles, email, passwords).
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Registers a new user in the Identity Store.
    /// </summary>
    Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)> RegisterUserAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates user credentials (email + password) without issuing tokens.
    /// </summary>
    Task<bool> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    Task<ApplicationUser?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles assigned to the specified user.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserRolesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to the specified user.
    /// </summary>
    Task<bool> AssignRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from the specified user.
    /// </summary>
    Task<bool> RemoveRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user's email has been confirmed.
    /// </summary>
    Task<bool> IsEmailConfirmedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an email confirmation token for the specified user.
    /// </summary>
    Task<string> GenerateEmailConfirmationTokenAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a user's email using the provided token.
    /// </summary>
    Task<bool> ConfirmEmailAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password reset token for the specified email.
    /// </summary>
    Task<string> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password using the provided token.
    /// </summary>
    Task<bool> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password after validating the current password.
    /// </summary>
    Task<bool> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's profile details (first name, last name).
    /// </summary>
    Task<bool> UpdateUserAsync(
        string userId,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes made to an ApplicationUser entity.
    /// </summary>
    Task<bool> SaveUserAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users that hold the specified role.
    /// </summary>
    Task<IReadOnlyList<ApplicationUser>> GetUsersInRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified user is currently locked out.
    /// </summary>
    Task<bool> IsLockedOutAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the lockout end date for the specified user, or null if not locked out.
    /// </summary>
    Task<DateTimeOffset?> GetLockoutEndDateAsync(
        string email,
        CancellationToken cancellationToken = default);
}

using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service responsible for generating, refreshing, and revoking JWT tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a new access token and refresh token pair for the specified user.
    /// </summary>
    Task<AuthTokenResponse> GenerateTokensAsync(
        ApplicationUser user,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Implements token rotation by invalidating the previous refresh token.
    /// </summary>
    Task<AuthTokenResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token with a reason.
    /// </summary>
    Task RevokeTokenAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user (e.g., on password reset or security event).
    /// </summary>
    Task RevokeAllUserTokensAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken = default);
}

using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for managing user sessions across devices.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for the user with device/browser information.
    /// </summary>
    Task<UserSession> CreateSessionAsync(
        string userId,
        string refreshTokenId,
        SessionInfo info,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for the specified user.
    /// </summary>
    Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session by its identifier.
    /// </summary>
    Task RevokeSessionAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a user, optionally excluding a specific session.
    /// </summary>
    Task RevokeAllSessionsAsync(
        string userId,
        Guid? exceptSessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last activity timestamp for a session.
    /// </summary>
    Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

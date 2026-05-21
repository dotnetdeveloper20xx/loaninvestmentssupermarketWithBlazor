using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Service for managing user sessions across devices.
/// Handles session creation, listing, revocation, activity tracking, and automatic cleanup.
/// </summary>
public sealed class SessionService : ISessionService
{
    private readonly AuthIdentityDbContext _dbContext;
    private readonly AccountSettings _accountSettings;

    public SessionService(
        AuthIdentityDbContext dbContext,
        IOptions<AccountSettings> accountSettings)
    {
        _dbContext = dbContext;
        _accountSettings = accountSettings.Value;
    }

    /// <inheritdoc />
    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string refreshTokenId,
        SessionInfo info,
        CancellationToken cancellationToken = default)
    {
        var session = new UserSession
        {
            UserId = userId,
            RefreshTokenId = refreshTokenId,
            DeviceType = info.DeviceType,
            IpAddress = info.IpAddress,
            Browser = info.Browser,
            CreatedAtUtc = DateTime.UtcNow,
            LastActivityAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return session;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Clean up inactive sessions beyond the timeout before returning results
        await CleanupInactiveSessionsAsync(userId, cancellationToken);

        var sessions = await _dbContext.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAtUtc)
            .Select(s => new UserSessionDto(
                s.Id,
                s.DeviceType,
                s.IpAddress,
                s.Browser,
                s.CreatedAtUtc,
                s.LastActivityAtUtc,
                s.IsActive))
            .ToListAsync(cancellationToken);

        return sessions;
    }

    /// <inheritdoc />
    public async Task RevokeSessionAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.UserId == userId,
                cancellationToken);

        if (session is null)
        {
            return;
        }

        session.IsActive = false;

        // Revoke the associated refresh token
        await RevokeRefreshTokenForSessionAsync(session.RefreshTokenId, "Session revoked", cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllSessionsAsync(
        string userId,
        Guid? exceptSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var sessionsQuery = _dbContext.UserSessions
            .Where(s => s.UserId == userId && s.IsActive);

        if (exceptSessionId.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => s.Id != exceptSessionId.Value);
        }

        var sessions = await sessionsQuery.ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;

            // Revoke the associated refresh token for each session
            await RevokeRefreshTokenForSessionAsync(session.RefreshTokenId, "All sessions revoked", cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.LastActivityAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically terminates sessions that have been inactive beyond the configured timeout.
    /// </summary>
    private async Task CleanupInactiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var timeoutThreshold = DateTime.UtcNow.AddMinutes(-_accountSettings.SessionInactivityTimeoutMinutes);

        var inactiveSessions = await _dbContext.UserSessions
            .Where(s => s.UserId == userId
                        && s.IsActive
                        && s.LastActivityAtUtc < timeoutThreshold)
            .ToListAsync(cancellationToken);

        foreach (var session in inactiveSessions)
        {
            session.IsActive = false;

            await RevokeRefreshTokenForSessionAsync(
                session.RefreshTokenId,
                "Session expired due to inactivity",
                cancellationToken);
        }

        if (inactiveSessions.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Revokes a refresh token by its ID string (which is stored as the token's Id.ToString()).
    /// </summary>
    private async Task RevokeRefreshTokenForSessionAsync(
        string refreshTokenId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(refreshTokenId, out var tokenGuid))
        {
            return;
        }

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenGuid, cancellationToken);

        if (refreshToken is not null && refreshToken.RevokedAtUtc is null)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            refreshToken.RevokedReason = reason;
        }
    }
}

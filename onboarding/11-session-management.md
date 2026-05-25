# 11 — Session Management

## Table of Contents

1. [UserSession Entity](#usersession-entity)
2. [Login Creates Session + Refresh Token](#login-creates-session--refresh-token)
3. [SessionService Implementation](#sessionservice-implementation)
4. [Get Active Sessions Query](#get-active-sessions-query)
5. [Revoke Single Session](#revoke-single-session)
6. [Revoke All Sessions](#revoke-all-sessions)
7. [Session Inactivity Timeout](#session-inactivity-timeout)
8. [How Sessions Relate to Refresh Tokens](#how-sessions-relate-to-refresh-tokens)
9. [The Sessions.razor Page](#the-sessionsrazor-page)
10. [Configuration](#configuration)

---

## UserSession Entity

Each login creates a session record that tracks the device, location, and activity:

```csharp
// src/LoanSuperMarket.Domain/Entities/Identity/UserSession.cs
using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class UserSession : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string RefreshTokenId { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
```

### Field Descriptions

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | Guid | Unique session identifier (from BaseEntity) |
| `UserId` | string | The user this session belongs to |
| `RefreshTokenId` | string | Links to the refresh token that powers this session |
| `DeviceType` | string? | "Desktop", "Mobile", "Tablet" |
| `IpAddress` | string? | Client IP (max 45 chars for IPv6) |
| `Browser` | string? | Browser user-agent string |
| `CreatedAtUtc` | DateTime | When the session was created (login time) |
| `LastActivityAtUtc` | DateTime | Last time the session was active |
| `IsActive` | bool | Whether the session is still valid |

### Database Configuration

```csharp
// In AuthIdentityDbContext.ConfigureUserSession()
modelBuilder.Entity<UserSession>(entity =>
{
    entity.ToTable("UserSessions");
    entity.HasKey(e => e.Id);

    entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
    entity.HasIndex(e => e.UserId);

    entity.Property(e => e.RefreshTokenId).IsRequired().HasMaxLength(450);
    entity.Property(e => e.DeviceType).HasMaxLength(100);
    entity.Property(e => e.IpAddress).HasMaxLength(45);
    entity.Property(e => e.Browser).HasMaxLength(256);

    entity.HasOne(e => e.User)
        .WithMany(u => u.Sessions)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

---

## Login Creates Session + Refresh Token

During login, after credentials are validated and tokens are generated, a session is
created to track the login:

```csharp
// From LoginCommandHandler.Handle()
public async Task<ApiResponse<AuthTokenResponse>> Handle(
    LoginCommand request, CancellationToken cancellationToken)
{
    // ... credential validation, account status check, 2FA ...

    // Generate tokens (access + refresh)
    var tokenResponse = await _tokenService.GenerateTokensAsync(
        user, request.RememberMe, cancellationToken);

    // Create session linked to the refresh token
    var sessionInfo = new SessionInfo(
        DeviceType: null,           // Could be parsed from User-Agent
        IpAddress: ipAddress,       // From IClientInfoProvider
        Browser: null               // Could be parsed from User-Agent
    );

    await _sessionService.CreateSessionAsync(
        user.Id,
        tokenResponse.RefreshToken,  // The refresh token string as the link
        sessionInfo,
        cancellationToken);

    // Update last login timestamp
    user.LastLoginAtUtc = DateTime.UtcNow;

    return ApiResponse<AuthTokenResponse>.Ok(tokenResponse, "Login successful.");
}
```

### SessionInfo Model

```csharp
// src/LoanSuperMarket.Application/Features/Auth/Models/SessionInfo.cs
namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Information about the client device/browser for session tracking.
/// </summary>
public sealed record SessionInfo(
    string? DeviceType,
    string? IpAddress,
    string? Browser);
```

### The Relationship

```
Login
  │
  ├── Creates RefreshToken (stored in DB)
  │     └── Token: "abc123..." (random 64 bytes)
  │
  └── Creates UserSession
        └── RefreshTokenId: "abc123..." (links to the token)
```

When a session is revoked, its linked refresh token is also revoked. When a refresh
token is rotated, the session remains active (it tracks the logical session, not a
specific token).

---

## SessionService Implementation

```csharp
// src/LoanSuperMarket.Infrastructure/Identity/SessionService.cs
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanSuperMarket.Infrastructure.Identity;

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

    /// <summary>
    /// Creates a new session record when a user logs in.
    /// </summary>
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

    /// <summary>
    /// Gets all active sessions for a user, cleaning up inactive ones first.
    /// </summary>
    public async Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Clean up expired sessions before returning results
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

    /// <summary>
    /// Revokes a single session and its associated refresh token.
    /// </summary>
    public async Task RevokeSessionAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.UserId == userId,
                cancellationToken);

        if (session is null) return;

        session.IsActive = false;

        // Also revoke the linked refresh token
        await RevokeRefreshTokenForSessionAsync(
            session.RefreshTokenId, "Session revoked", cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Revokes all sessions for a user, optionally keeping one active.
    /// </summary>
    public async Task RevokeAllSessionsAsync(
        string userId,
        Guid? exceptSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var sessionsQuery = _dbContext.UserSessions
            .Where(s => s.UserId == userId && s.IsActive);

        if (exceptSessionId.HasValue)
        {
            sessionsQuery = sessionsQuery
                .Where(s => s.Id != exceptSessionId.Value);
        }

        var sessions = await sessionsQuery.ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;
            await RevokeRefreshTokenForSessionAsync(
                session.RefreshTokenId, "All sessions revoked", cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the last activity timestamp for a session.
    /// </summary>
    public async Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive, cancellationToken);

        if (session is null) return;

        session.LastActivityAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Get Active Sessions Query

### UserSessionDto

```csharp
// src/LoanSuperMarket.Application/Features/Auth/Models/UserSessionDto.cs
namespace LoanSuperMarket.Application.Features.Auth.Models;

public sealed record UserSessionDto(
    Guid Id,
    string? DeviceType,
    string? IpAddress,
    string? Browser,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc,
    bool IsActive);
```

### API Endpoint

```csharp
[Authorize]
[HttpGet("api/sessions/my")]
public async Task<IActionResult> GetMySessions()
{
    var userId = _currentUserService.UserId;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    var sessions = await _sessionService.GetUserSessionsAsync(userId);
    return Ok(ApiResponse<IReadOnlyList<UserSessionDto>>.Ok(sessions));
}
```

### What the Response Looks Like

```json
{
  "success": true,
  "data": [
    {
      "id": "a1b2c3d4-...",
      "deviceType": "Desktop",
      "ipAddress": "192.168.1.100",
      "browser": "Chrome 120",
      "createdAtUtc": "2024-01-15T10:30:00Z",
      "lastActivityAtUtc": "2024-01-15T14:22:00Z",
      "isActive": true
    },
    {
      "id": "e5f6g7h8-...",
      "deviceType": "Mobile",
      "ipAddress": "10.0.0.50",
      "browser": "Safari iOS",
      "createdAtUtc": "2024-01-14T08:00:00Z",
      "lastActivityAtUtc": "2024-01-14T18:45:00Z",
      "isActive": true
    }
  ],
  "errors": []
}
```

---

## Revoke Single Session

A user can revoke any of their own sessions (e.g., "I left my account logged in at work"):

### API Endpoint

```csharp
[Authorize]
[HttpPost("api/sessions/{sessionId}/revoke")]
public async Task<IActionResult> RevokeSession(Guid sessionId)
{
    var userId = _currentUserService.UserId;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    await _sessionService.RevokeSessionAsync(sessionId, userId);
    return Ok(ApiResponse<string>.Ok("Session revoked successfully."));
}
```

### What Happens When a Session Is Revoked

1. `UserSession.IsActive` is set to `false`
2. The linked `RefreshToken` is revoked (cannot be used for refresh)
3. The access token remains valid until it expires (max 15 minutes)
4. On next API call after access token expires, the client gets 401
5. Client tries to refresh → fails (token revoked) → redirected to login

```
Session Revoked
     │
     ├── UserSession.IsActive = false
     │
     └── RefreshToken.RevokedAtUtc = now
              │
              └── Next refresh attempt fails
                       │
                       └── Client redirected to /login
```

---

## Revoke All Sessions

"Log me out everywhere" — revokes all sessions except optionally the current one:

### API Endpoint

```csharp
[Authorize]
[HttpPost("api/sessions/revoke-all")]
public async Task<IActionResult> RevokeAllSessions()
{
    var userId = _currentUserService.UserId;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    // Revoke ALL sessions (including current)
    await _sessionService.RevokeAllSessionsAsync(userId);
    return Ok(ApiResponse<string>.Ok("All sessions revoked."));
}
```

### The `exceptSessionId` Parameter

The `RevokeAllSessionsAsync` method accepts an optional `exceptSessionId` to keep
the current session alive:

```csharp
public async Task RevokeAllSessionsAsync(
    string userId,
    Guid? exceptSessionId = null,  // Keep this session active
    CancellationToken cancellationToken = default)
{
    var sessionsQuery = _dbContext.UserSessions
        .Where(s => s.UserId == userId && s.IsActive);

    if (exceptSessionId.HasValue)
    {
        sessionsQuery = sessionsQuery
            .Where(s => s.Id != exceptSessionId.Value);
    }

    var sessions = await sessionsQuery.ToListAsync(cancellationToken);

    foreach (var session in sessions)
    {
        session.IsActive = false;
        await RevokeRefreshTokenForSessionAsync(
            session.RefreshTokenId, "All sessions revoked", cancellationToken);
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
}
```

**Use case:** "Log me out of all other devices" — pass the current session ID as
`exceptSessionId` to keep the user logged in on their current device.

---

## Session Inactivity Timeout

Sessions automatically expire after a period of inactivity. This is configured in
`AccountSettings`:

```json
// appsettings.json
{
  "AccountSettings": {
    "SessionInactivityTimeoutMinutes": 30
  }
}
```

```csharp
// src/LoanSuperMarket.Shared/Configuration/AccountSettings.cs
public sealed class AccountSettings
{
    public const string SectionName = "AccountSettings";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int MaxActiveLoansPerBorrower { get; set; } = 5;
    public int SessionInactivityTimeoutMinutes { get; set; } = 30;
}
```

### How Cleanup Works

The `CleanupInactiveSessionsAsync` method runs automatically when sessions are queried:

```csharp
    private async Task CleanupInactiveSessionsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var timeoutThreshold = DateTime.UtcNow
            .AddMinutes(-_accountSettings.SessionInactivityTimeoutMinutes);

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
```

### When Does Cleanup Run?

Cleanup is triggered **lazily** — when `GetUserSessionsAsync` is called. This means:
- No background job needed
- Sessions are cleaned up before the user sees them
- Stale sessions don't accumulate indefinitely

### Activity Tracking

The `UpdateActivityAsync` method should be called periodically to keep sessions alive:

```csharp
    public async Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive, cancellationToken);

        if (session is null) return;

        session.LastActivityAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
```

This could be called:
- On every token refresh (most common)
- On significant user actions (loan application, payment)
- Via a periodic heartbeat from the Blazor client

---

## How Sessions Relate to Refresh Tokens

Sessions and refresh tokens are **linked but separate concepts**:

| Concept | Purpose | Lifetime |
|---------|---------|----------|
| **Refresh Token** | Proves the client can get new access tokens | 7-30 days |
| **User Session** | Tracks where/when the user is logged in | Until revoked or inactive |

### The Link

```csharp
// UserSession stores the refresh token string as RefreshTokenId
public string RefreshTokenId { get; set; } = string.Empty;
```

When a session is revoked, the linked refresh token is also revoked:

```csharp
    private async Task RevokeRefreshTokenForSessionAsync(
        string refreshTokenId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(refreshTokenId, out var tokenGuid))
            return;

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenGuid, cancellationToken);

        if (refreshToken is not null && refreshToken.RevokedAtUtc is null)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            refreshToken.RevokedReason = reason;
        }
    }
```

### Token Rotation and Sessions

When a refresh token is rotated (old revoked, new issued), the session remains active.
The session represents the logical login, while tokens are the mechanism:

```
Login
  └── Session (logical login, tracks device/IP)
        └── RefreshToken_1 (initial)
              └── Rotated → RefreshToken_2
                    └── Rotated → RefreshToken_3
                          └── ... (session stays active)
```

### Revocation Cascade

```
Revoke Session
  │
  ├── Session.IsActive = false
  └── RefreshToken.RevokedAtUtc = now
        └── Next refresh attempt → 401
              └── Client → /login

Revoke All User Tokens (reuse detection)
  │
  ├── ALL RefreshTokens revoked
  └── Sessions become effectively dead
        └── Next activity check → cleaned up
```

---

## The Sessions.razor Page

The Blazor client provides a UI for users to view and manage their sessions:

```razor
@* src/LoanSuperMarket.Blazor/Pages/Account/Sessions.razor *@
@page "/account/sessions"
@using LoanSuperMarket.Shared.Users
@using LoanSuperMarket.Shared.Common
@attribute [Microsoft.AspNetCore.Authorization.Authorize]
@inject HttpClient Http

<PageHeader Title="Active Sessions"
            Subtitle="View and manage your active sessions across devices." />

@if (!string.IsNullOrWhiteSpace(_successMessage))
{
    <div class="mb-6 rounded-2xl border border-green-200 bg-green-50 p-4 text-sm text-green-700">
        @_successMessage
    </div>
}

@if (!string.IsNullOrWhiteSpace(_errorMessage))
{
    <div class="mb-6 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
        @_errorMessage
    </div>
}

<div class="rounded-2xl border border-slate-200 bg-white shadow-sm">
    <div class="flex items-center justify-between border-b border-slate-200 px-6 py-4">
        <div>
            <h2 class="text-lg font-semibold text-slate-900">Sessions</h2>
            <p class="mt-1 text-sm text-slate-500">
                @if (_sessions.Count > 0)
                {
                    @($"{_sessions.Count(s => s.IsActive)} active session(s)")
                }
                else
                {
                    <span>No sessions found</span>
                }
            </p>
        </div>
        @if (_sessions.Count > 1)
        {
            <button class="rounded-lg border border-red-200 bg-white px-4 py-2.5
                           text-sm font-semibold text-red-700 hover:bg-red-50
                           disabled:opacity-50"
                    disabled="@_isRevoking"
                    @onclick="RevokeAllSessionsAsync">
                Revoke All Sessions
            </button>
        }
    </div>

    @if (_isLoading)
    {
        <div class="flex items-center justify-center py-12">
            <p class="text-sm text-slate-500">Loading sessions...</p>
        </div>
    }
    else if (_sessions.Count == 0)
    {
        <div class="flex flex-col items-center justify-center py-12">
            <span class="text-4xl">🖥️</span>
            <p class="mt-3 text-sm font-medium text-slate-700">No active sessions</p>
        </div>
    }
    else
    {
        <table class="w-full">
            <thead class="border-b border-slate-200 bg-slate-50">
                <tr>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">Device</th>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">IP Address</th>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">Browser</th>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">Created</th>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">Last Activity</th>
                    <th class="px-6 py-4 text-left text-xs font-semibold uppercase">Status</th>
                    <th class="px-6 py-4 text-right text-xs font-semibold uppercase">Actions</th>
                </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
                @foreach (var session in _sessions)
                {
                    <tr class="hover:bg-slate-50">
                        <td class="px-6 py-4">
                            <span class="text-lg">@GetDeviceIcon(session.DeviceType)</span>
                            <span class="text-sm font-medium">
                                @(session.DeviceType ?? "Unknown")
                            </span>
                        </td>
                        <td class="px-6 py-4 text-sm">@(session.IpAddress ?? "Unknown")</td>
                        <td class="px-6 py-4 text-sm">@(session.Browser ?? "Unknown")</td>
                        <td class="px-6 py-4 text-sm">
                            @session.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                        </td>
                        <td class="px-6 py-4 text-sm">
                            @session.LastActivityAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                        </td>
                        <td class="px-6 py-4">
                            @if (session.IsActive)
                            {
                                <span class="rounded-full bg-green-100 px-2.5 py-1
                                             text-xs font-medium text-green-700">
                                    Active
                                </span>
                            }
                        </td>
                        <td class="px-6 py-4 text-right">
                            @if (session.IsActive)
                            {
                                <button class="rounded-lg border border-red-200 px-3 py-1.5
                                               text-xs font-semibold text-red-700
                                               hover:bg-red-50 disabled:opacity-50"
                                        disabled="@_isRevoking"
                                        @onclick="() => RevokeSessionAsync(session.Id)">
                                    Revoke
                                </button>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>
```

### Code-Behind Logic

```csharp
@code {
    private List<UserSessionDto> _sessions = [];
    private bool _isLoading = true;
    private bool _isRevoking;
    private string? _errorMessage;
    private string? _successMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        _successMessage = null;

        try
        {
            var response = await Http.GetFromJsonAsync<
                ApiResponse<IReadOnlyList<UserSessionDto>>>("api/sessions/my");

            if (response?.Success == true)
                _sessions = response.Data?.ToList() ?? [];
            else
                _errorMessage = "Failed to load sessions.";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Unable to load sessions. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RevokeSessionAsync(Guid sessionId)
    {
        _isRevoking = true;
        try
        {
            var response = await Http.PostAsync(
                $"api/sessions/{sessionId}/revoke", null);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponse<string>>();

            if (result?.Success == true)
            {
                _successMessage = "Session revoked successfully.";
                await LoadSessionsAsync();
            }
            else
            {
                _errorMessage = "Failed to revoke session.";
            }
        }
        finally
        {
            _isRevoking = false;
        }
    }

    private async Task RevokeAllSessionsAsync()
    {
        _isRevoking = true;
        try
        {
            var response = await Http.PostAsync("api/sessions/revoke-all", null);
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponse<string>>();

            if (result?.Success == true)
            {
                _successMessage = "All sessions revoked successfully.";
                await LoadSessionsAsync();
            }
            else
            {
                _errorMessage = "Failed to revoke all sessions.";
            }
        }
        finally
        {
            _isRevoking = false;
        }
    }

    private static string GetDeviceIcon(string? deviceType) => deviceType?.ToLowerInvariant() switch
    {
        "desktop" => "🖥️",
        "mobile" => "📱",
        "tablet" => "📱",
        _ => "💻"
    };
}
```

### UI Features

1. **Session list** — Shows all active sessions with device, IP, browser, timestamps
2. **Revoke single** — Terminates one specific session (e.g., "log out my phone")
3. **Revoke all** — Nuclear option, terminates everything
4. **Auto-refresh** — After revoking, the list reloads to show updated state
5. **Loading/error states** — Proper UX for async operations
6. **Disabled during operations** — Prevents double-clicks

---

## Configuration

### Complete Settings

```json
{
  "AccountSettings": {
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "MaxActiveLoansPerBorrower": 5,
    "SessionInactivityTimeoutMinutes": 30
  },
  "JwtSettings": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "RememberMeRefreshTokenExpirationDays": 30
  }
}
```

### How Settings Interact

| Setting | Effect on Sessions |
|---------|-------------------|
| `SessionInactivityTimeoutMinutes: 30` | Sessions with no activity for 30 min are auto-terminated |
| `AccessTokenExpirationMinutes: 15` | After session revoke, max 15 min until forced logout |
| `RefreshTokenExpirationDays: 7` | Even active sessions expire after 7 days without "Remember Me" |
| `RememberMeRefreshTokenExpirationDays: 30` | "Remember Me" sessions last up to 30 days |

### DI Registration

```csharp
// In DependencyInjection.cs
services.AddScoped<ISessionService, SessionService>();

// AccountSettings binding
services.Configure<AccountSettings>(
    configuration.GetSection(AccountSettings.SectionName));
```

---

## Summary: Session Lifecycle

```
1. User logs in
   └── LoginCommandHandler creates:
       ├── Access Token (15 min)
       ├── Refresh Token (7 or 30 days)
       └── UserSession (linked to refresh token)

2. User makes API calls
   └── Access token attached by AuthTokenHandler
       └── On 401: refresh token used to get new access token
           └── Session stays active

3. Session activity tracked
   └── LastActivityAtUtc updated on significant actions
       └── If no activity for 30 min → session auto-terminated

4. User views sessions page
   └── GET /api/sessions/my
       └── Cleanup runs first (removes inactive sessions)
           └── Returns active sessions

5. User revokes a session
   └── POST /api/sessions/{id}/revoke
       └── Session.IsActive = false
           └── Linked RefreshToken revoked
               └── That device forced to re-login

6. User revokes all sessions
   └── POST /api/sessions/revoke-all
       └── All sessions deactivated
           └── All refresh tokens revoked
               └── All devices forced to re-login

7. Security event (token reuse detected)
   └── JwtTokenService.RevokeAllUserTokensAsync()
       └── All refresh tokens revoked
           └── Sessions become dead (cleaned up on next query)
```

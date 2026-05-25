# 10 — Account Lifecycle and Vetting

## Overview

Every user account in the Loan Investment Supermarket goes through a defined lifecycle from registration to potential closure. The system enforces account status restrictions at the **MediatR pipeline level**, meaning no command or query handler needs to manually check account status — it's handled automatically.

---

## Account Status State Machine

```
                    ┌─────────────────┐
                    │   Registration   │
                    └────────┬────────┘
                             │
                             ▼
                ┌────────────────────────┐
                │    PendingApproval      │ ← Default status on registration
                └────────────┬───────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
    ┌──────────────┐  ┌───────────┐  ┌──────────────────┐
    │   Active     │  │  Closed   │  │ DocumentsRequested│
    │              │  │ (rejected)│  │                    │
    └──────┬───────┘  └───────────┘  └────────┬─────────┘
           │                                    │
           │         ┌──────────────────────────┘
           │         │ (docs submitted → re-review)
           │         ▼
           │    Back to PendingApproval
           │
    ┌──────┼──────────────────────────────┐
    │      │                              │
    ▼      ▼              ▼               ▼
┌──────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐
│ Hold │ │ Blocked │ │Suspended │ │  Closed  │
└──────┘ └─────────┘ └──────────┘ └──────────┘
```

---

## The AccountStatus Enum

**File:** `src/LoanSuperMarket.Domain/Enums/AccountStatus.cs`

```csharp
public enum AccountStatus
{
    PendingApproval,
    Active,
    Hold,
    Blocked,
    Suspended,
    Closed,
    DocumentsRequested
}
```

| Status | What the User Can Do | Who Sets It |
|--------|---------------------|-------------|
| `PendingApproval` | View profile only (via `IAllowPendingApproval`) | System (on registration) |
| `Active` | Full access based on role | CrmManager (on approval) |
| `Hold` | Everything except create new loans/products | Admin/CrmManager |
| `Blocked` | Everything except blocked activity (Borrowing/Lending/Both) | Admin |
| `Suspended` | Nothing — all access denied | Admin |
| `Closed` | Nothing — account permanently closed | Admin/CrmManager (on rejection) |
| `DocumentsRequested` | Same as Active (can upload docs) | CrmManager |

---

## Registration Flow

### RegisterCommand

**File:** `src/LoanSuperMarket.Application/Features/Auth/Commands/Register/RegisterCommand.cs`

```csharp
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType,          // "Borrower" or "Lender"
    string? CompanyName = null) : IRequest<ApiResponse<string>>;
```

### RegisterCommandHandler

**File:** `src/LoanSuperMarket.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs`

```csharp
public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Create user via Identity (sets AccountStatus to Active internally)
        var registerRequest = new RegisterUserRequest(
            request.Email, request.Password,
            request.FirstName, request.LastName,
            request.UserType, request.CompanyName);

        var (succeeded, userId, errors) = await _identityService.RegisterUserAsync(
            registerRequest, cancellationToken);

        if (!succeeded)
            return ApiResponse<string>.Fail(errors.ToList());

        // 2. Assign role based on UserType ("Borrower" or "Lender")
        await _identityService.AssignRoleAsync(userId, request.UserType, cancellationToken);

        // 3. Generate and auto-confirm email (dev mode — no email provider)
        var confirmationToken = await _identityService
            .GenerateEmailConfirmationTokenAsync(userId, cancellationToken);
        await _identityService.ConfirmEmailAsync(userId, confirmationToken, cancellationToken);

        // 4. Send confirmation email (no-op in dev)
        await _emailService.SendEmailConfirmationAsync(
            request.Email, userId, confirmationToken, cancellationToken);

        // 5. Record audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create("ApplicationUser", null, "Registered",
                $"New {request.UserType} registered with email {request.Email}."),
            cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(userId, "Registration successful.");
    }
}
```

### Registration Flow Summary

```
1. User submits registration form (email, password, name, userType)
2. RegisterCommand → MediatR pipeline
3. IdentityService creates ApplicationUser with AccountStatus = Active
4. Role assigned (Borrower or Lender)
5. Email auto-confirmed (development mode)
6. Audit log recorded
7. User can immediately log in and use the platform
```

**Note:** In the current implementation, users are set to `Active` immediately. In a production vetting workflow, you would change this to `PendingApproval` and require CRM review before activation.

---

## Vetting Queue (CRM Reviews Pending Registrations)

### VettingController

**File:** `src/LoanSuperMarket.Api/Controllers/VettingController.cs`

```csharp
[ApiController]
[Route("api/vetting")]
[Authorize(Policy = "CanVetUsers")]  // Only CrmManager
public sealed class VettingController : ControllerBase
{
    private readonly ISender _sender;

    public VettingController(ISender sender) => _sender = sender;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<PagedResult<VettingItemDto>>>> GetVettingQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetVettingQueueQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponse<PagedResult<VettingItemDto>>.Ok(result));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
        string id,
        [FromBody] ApproveRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveRegistrationCommand(
            id, request.Reason, request.CreditTier,
            request.CreditLimit, request.CapitalLimit);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        string id,
        [FromBody] RejectRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectRegistrationCommand(id, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/request-docs")]
    public async Task<ActionResult<ApiResponse<string>>> RequestDocuments(
        string id,
        [FromBody] VettingRequestDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestDocumentsCommand(id, request.RequiredDocuments);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
```

### Request DTOs

```csharp
public sealed record ApproveRegistrationRequest(
    string Reason,
    CreditTier? CreditTier = null,
    decimal? CreditLimit = null,
    decimal? CapitalLimit = null);

public sealed record RejectRegistrationRequest(string Reason);

public sealed record VettingRequestDocumentsRequest(IReadOnlyList<string> RequiredDocuments);
```

---

## Approve Registration Command

**File:** `src/LoanSuperMarket.Application/Features/Vetting/Commands/ApproveRegistration/ApproveRegistrationCommand.cs`

```csharp
public sealed record ApproveRegistrationCommand(
    string UserId,
    string Reason,
    CreditTier? CreditTier = null,
    decimal? CreditLimit = null,
    decimal? CapitalLimit = null) : IRequest<ApiResponse<string>>;
```

### ApproveRegistrationCommandHandler

```csharp
public sealed class ApproveRegistrationCommandHandler
    : IRequestHandler<ApproveRegistrationCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public async Task<ApiResponse<string>> Handle(
        ApproveRegistrationCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return ApiResponse<string>.Fail("User not found.");

        // Only PendingApproval or DocumentsRequested can be approved
        if (user.AccountStatus is not (AccountStatus.PendingApproval
            or AccountStatus.DocumentsRequested))
        {
            return ApiResponse<string>.Fail(
                $"User cannot be approved from status '{user.AccountStatus}'.");
        }

        // Validate required fields based on user type
        var roles = await _identityService.GetUserRolesAsync(request.UserId, cancellationToken);
        var isBorrower = roles.Contains("Borrower");
        var isLender = roles.Contains("Lender");

        if (isBorrower && request.CreditTier is null)
            return ApiResponse<string>.Fail("CreditTier is required for Borrowers.");
        if (isBorrower && request.CreditLimit is null)
            return ApiResponse<string>.Fail("CreditLimit is required for Borrowers.");
        if (isLender && request.CapitalLimit is null)
            return ApiResponse<string>.Fail("CapitalLimit is required for Lenders.");

        // Update account status
        user.AccountStatus = AccountStatus.Active;
        user.AccountStatusReason = request.Reason;
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        // Set credit/capital limits
        if (isBorrower)
        {
            user.CreditTier = request.CreditTier;
            user.CreditLimit = request.CreditLimit;
        }
        if (isLender)
        {
            user.CapitalLimit = request.CapitalLimit;
        }

        await _identityService.SaveUserAsync(user, cancellationToken);

        // Notify user and record audit log
        await _emailService.SendAccountStatusChangedAsync(
            user.Email!, user.FullName,
            AccountStatus.PendingApproval, AccountStatus.Active,
            request.Reason, cancellationToken);

        await _auditLogRepository.AddAsync(
            AuditLog.Create("ApplicationUser", null, "RegistrationApproved",
                $"Registration approved for '{user.Email}'."),
            cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(request.UserId, "Registration approved.");
    }
}
```

---

## Reject Registration Command

**File:** `src/LoanSuperMarket.Application/Features/Vetting/Commands/RejectRegistration/RejectRegistrationCommand.cs`

```csharp
public sealed record RejectRegistrationCommand(
    string UserId,
    string Reason) : IRequest<ApiResponse<string>>;
```

### RejectRegistrationCommandHandler

```csharp
public sealed class RejectRegistrationCommandHandler
    : IRequestHandler<RejectRegistrationCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public async Task<ApiResponse<string>> Handle(
        RejectRegistrationCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return ApiResponse<string>.Fail("User not found.");

        // Only PendingApproval or DocumentsRequested can be rejected
        if (user.AccountStatus is not (AccountStatus.PendingApproval
            or AccountStatus.DocumentsRequested))
        {
            return ApiResponse<string>.Fail(
                $"User cannot be rejected from status '{user.AccountStatus}'.");
        }

        // Set status to Closed (permanently rejected)
        user.AccountStatus = AccountStatus.Closed;
        user.AccountStatusReason = request.Reason;
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        await _identityService.SaveUserAsync(user, cancellationToken);

        // Notify user
        await _emailService.SendAccountStatusChangedAsync(
            user.Email!, user.FullName,
            AccountStatus.PendingApproval, AccountStatus.Closed,
            request.Reason, cancellationToken);

        // Audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create("ApplicationUser", null, "RegistrationRejected",
                $"Registration rejected for '{user.Email}'. Reason: {request.Reason}."),
            cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(request.UserId, "Registration rejected.");
    }
}
```

---

## AccountStatusBehaviour — Pipeline Enforcement

**File:** `src/LoanSuperMarket.Application/Common/Behaviours/AccountStatusBehaviour.cs`

This is the **core enforcement mechanism**. It runs as a MediatR pipeline behaviour on EVERY command and query, automatically blocking requests based on the user's account status.

```csharp
public sealed class AccountStatusBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public AccountStatusBehaviour(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip for unauthenticated requests (login, register)
        if (!_currentUserService.IsAuthenticated
            || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await next(cancellationToken);
        }

        var user = await _identityService.GetUserByIdAsync(
            _currentUserService.UserId, cancellationToken);

        if (user is null)
            return await next(cancellationToken);

        switch (user.AccountStatus)
        {
            case AccountStatus.Closed:
                throw new AccountStatusException(
                    AccountStatus.Closed,
                    "AUTH_ACCOUNT_CLOSED",
                    "This account has been permanently closed.");

            case AccountStatus.Suspended:
                throw new AccountStatusException(
                    AccountStatus.Suspended,
                    "AUTH_ACCOUNT_SUSPENDED",
                    "This account has been suspended.");

            case AccountStatus.PendingApproval:
                EnforcePendingApproval(request);
                break;

            case AccountStatus.Hold:
                EnforceHold(request);
                break;

            case AccountStatus.Blocked:
                EnforceBlocked(request, user.BlockedActivity);
                break;

            case AccountStatus.Active:
            case AccountStatus.DocumentsRequested:
                break; // No restrictions
        }

        return await next(cancellationToken);
    }
}
```

### Pipeline Registration Order

```csharp
// In DependencyInjection.cs
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccountStatusBehaviour<,>));  // ← HERE
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LimitEnforcementBehaviour<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceAuthorizationBehaviour<,>));
```

The order matters:
1. Validation first (reject malformed requests early)
2. Caching (return cached responses without hitting the database)
3. **Account status** (block restricted accounts)
4. Limit enforcement (check credit/capital limits)
5. Resource authorization (scope data by user)

---

## Status-Specific Enforcement Rules

### PendingApproval — Only IAllowPendingApproval Requests

```csharp
private static void EnforcePendingApproval(TRequest request)
{
    if (request is IAllowPendingApproval)
        return;  // Allowed

    throw new AccountStatusException(
        AccountStatus.PendingApproval,
        "AUTH_PENDING_APPROVAL",
        "Your account is pending approval. You can only view your profile.");
}
```

### The IAllowPendingApproval Marker Interface

**File:** `src/LoanSuperMarket.Application/Common/Interfaces/IAllowPendingApproval.cs`

```csharp
namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries/commands that are allowed for users
/// with PendingApproval account status.
/// Typically profile-viewing queries implement this interface.
/// </summary>
public interface IAllowPendingApproval { }
```

**Usage:** Any query that a pending user should be able to execute implements this interface:

```csharp
// This query is allowed for PendingApproval users
public sealed record GetMyProfileQuery : IRequest<ApiResponse<UserProfileDto>>, IAllowPendingApproval;

// This command is NOT allowed (no marker interface)
public sealed record CreateLoanApplicationCommand(...) : IRequest<ApiResponse<Guid>>;
```

### Hold — Blocks New Loan/Product Creation

```csharp
private static void EnforceHold(TRequest request)
{
    if (request is ICreateLoanCommand)
    {
        throw new AccountStatusException(
            AccountStatus.Hold, "AUTH_ACCOUNT_HOLD",
            "Your account is on hold. You cannot create new loan applications.");
    }

    if (request is ICreateProductCommand)
    {
        throw new AccountStatusException(
            AccountStatus.Hold, "AUTH_ACCOUNT_HOLD",
            "Your account is on hold. You cannot create new loan products.");
    }
}
```

Commands that create loans implement `ICreateLoanCommand`:
```csharp
public interface ICreateLoanCommand { }

// Example:
public sealed record SubmitLoanApplicationCommand(...)
    : IRequest<ApiResponse<Guid>>, ICreateLoanCommand;
```

### Blocked — Activity-Specific Restrictions

```csharp
private static void EnforceBlocked(TRequest request, string? blockedActivity)
{
    if (string.IsNullOrEmpty(blockedActivity)) return;

    var isBorrowingBlocked = blockedActivity.Equals("Borrowing", StringComparison.OrdinalIgnoreCase)
                             || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);

    var isLendingBlocked = blockedActivity.Equals("Lending", StringComparison.OrdinalIgnoreCase)
                           || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);

    if (isBorrowingBlocked && request is ICreateLoanCommand)
        throw new AccountStatusException(AccountStatus.Blocked, "AUTH_ACCOUNT_BLOCKED",
            "Your account is blocked from borrowing activities.");

    if (isLendingBlocked && request is ICreateProductCommand)
        throw new AccountStatusException(AccountStatus.Blocked, "AUTH_ACCOUNT_BLOCKED",
            "Your account is blocked from lending activities.");
}
```

The `BlockedActivity` field on `ApplicationUser` can be:
- `"Borrowing"` — user can't create loan applications
- `"Lending"` — user can't create loan products
- `"Both"` — user can't do either

---

## Session Management

### UserSession Entity

**File:** `src/LoanSuperMarket.Domain/Entities/Identity/UserSession.cs`

```csharp
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

### ISessionService Interface

```csharp
public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(
        string userId, string refreshTokenId, SessionInfo info,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSessionDto>> GetUserSessionsAsync(
        string userId, CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(
        Guid sessionId, string userId,
        CancellationToken cancellationToken = default);

    Task RevokeAllSessionsAsync(
        string userId, Guid? exceptSessionId = null,
        CancellationToken cancellationToken = default);

    Task UpdateActivityAsync(
        Guid sessionId, CancellationToken cancellationToken = default);
}
```

### Session Creation (On Login)

When a user logs in successfully, a session is created:

```csharp
var session = new UserSession
{
    UserId = userId,
    RefreshTokenId = refreshTokenId,
    DeviceType = info.DeviceType,    // "Desktop", "Mobile", etc.
    IpAddress = info.IpAddress,       // Client IP
    Browser = info.Browser,           // "Chrome 120", etc.
    CreatedAtUtc = DateTime.UtcNow,
    LastActivityAtUtc = DateTime.UtcNow,
    IsActive = true
};

_dbContext.UserSessions.Add(session);
await _dbContext.SaveChangesAsync(cancellationToken);
```

### Session Revocation

Users can revoke individual sessions or all sessions:

```csharp
// Revoke a specific session (e.g., "log out this device")
public async Task RevokeSessionAsync(Guid sessionId, string userId, ...)
{
    var session = await _dbContext.UserSessions
        .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

    if (session is null) return;

    session.IsActive = false;

    // Also revoke the associated refresh token
    await RevokeRefreshTokenForSessionAsync(
        session.RefreshTokenId, "Session revoked", cancellationToken);

    await _dbContext.SaveChangesAsync(cancellationToken);
}

// Revoke all sessions except current (e.g., "log out everywhere else")
public async Task RevokeAllSessionsAsync(
    string userId, Guid? exceptSessionId = null, ...)
{
    var sessions = await _dbContext.UserSessions
        .Where(s => s.UserId == userId && s.IsActive)
        .Where(s => exceptSessionId == null || s.Id != exceptSessionId.Value)
        .ToListAsync(cancellationToken);

    foreach (var session in sessions)
    {
        session.IsActive = false;
        await RevokeRefreshTokenForSessionAsync(
            session.RefreshTokenId, "All sessions revoked", cancellationToken);
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
}
```

### Automatic Session Cleanup (Inactivity Timeout)

```csharp
private async Task CleanupInactiveSessionsAsync(string userId, CancellationToken ct)
{
    var timeoutThreshold = DateTime.UtcNow
        .AddMinutes(-_accountSettings.SessionInactivityTimeoutMinutes); // 30 min

    var inactiveSessions = await _dbContext.UserSessions
        .Where(s => s.UserId == userId
                    && s.IsActive
                    && s.LastActivityAtUtc < timeoutThreshold)
        .ToListAsync(ct);

    foreach (var session in inactiveSessions)
    {
        session.IsActive = false;
        await RevokeRefreshTokenForSessionAsync(
            session.RefreshTokenId, "Session expired due to inactivity", ct);
    }

    if (inactiveSessions.Count > 0)
        await _dbContext.SaveChangesAsync(ct);
}
```

Configuration:
```json
{
  "AccountSettings": {
    "SessionInactivityTimeoutMinutes": 30
  }
}
```

---

## How Account Status Affects What a User Can Do

### Complete Restriction Matrix

| Action | Active | PendingApproval | Hold | Blocked (Borrowing) | Blocked (Lending) | Suspended | Closed |
|--------|:------:|:---------------:|:----:|:-------------------:|:-----------------:|:---------:|:------:|
| View profile | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| View dashboard | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Create loan application | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Create loan product | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| View existing loans | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Make payments | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Upload documents | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Login | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### Error Codes Returned

| Status | Error Code | HTTP Status |
|--------|-----------|-------------|
| PendingApproval | `AUTH_PENDING_APPROVAL` | 403 |
| Hold | `AUTH_ACCOUNT_HOLD` | 403 |
| Blocked | `AUTH_ACCOUNT_BLOCKED` | 403 |
| Suspended | `AUTH_ACCOUNT_SUSPENDED` | 403 |
| Closed | `AUTH_ACCOUNT_CLOSED` | 403 |

---

## Vetting Workflow: End-to-End

### Happy Path (Borrower Approved)

```
1. Borrower registers → AccountStatus = Active (or PendingApproval in production)
2. CrmManager opens Vetting Queue → sees pending registrations
3. CrmManager reviews borrower's information
4. CrmManager approves with:
   - Reason: "Documents verified, credit check passed"
   - CreditTier: Standard
   - CreditLimit: 50000.00
5. System sets AccountStatus = Active
6. System sets CreditTier and CreditLimit on ApplicationUser
7. Email notification sent to borrower
8. Audit log recorded
9. Borrower can now create loan applications (up to CreditLimit)
```

### Rejection Path

```
1. Borrower registers → AccountStatus = PendingApproval
2. CrmManager reviews → finds issues
3. CrmManager rejects with:
   - Reason: "Failed AML check — suspicious activity detected"
4. System sets AccountStatus = Closed
5. Email notification sent to borrower
6. Audit log recorded
7. Borrower cannot log in or access any features
```

### Documents Requested Path

```
1. Borrower registers → AccountStatus = PendingApproval
2. CrmManager reviews → needs more documents
3. CrmManager requests documents:
   - RequiredDocuments: ["ID Proof", "Address Proof", "Income Statement"]
4. System sets AccountStatus = DocumentsRequested
5. Borrower can upload documents (DocumentsRequested allows this)
6. Once uploaded → CrmManager re-reviews
7. CrmManager approves or rejects
```

---

## Adding a New Account Status: Step-by-Step

1. **Add to the enum:**
   ```csharp
   public enum AccountStatus
   {
       // ... existing values
       UnderReview  // New status
   }
   ```

2. **Update AccountStatusBehaviour:**
   ```csharp
   case AccountStatus.UnderReview:
       EnforceUnderReview(request);
       break;
   ```

3. **Define enforcement rules:**
   ```csharp
   private static void EnforceUnderReview(TRequest request)
   {
       // Define what's allowed/blocked for this status
       if (request is not IAllowPendingApproval)
           throw new AccountStatusException(...);
   }
   ```

4. **Update the vetting commands** to allow transitions to/from the new status

5. **Update the Blazor UI** to display the new status appropriately

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Pipeline behaviour (not middleware) | Runs after authentication, has access to MediatR request type for marker interface checks |
| Marker interfaces (`IAllowPendingApproval`, `ICreateLoanCommand`) | Type-safe, compile-time checked, no magic strings |
| Status stored on ApplicationUser | Single source of truth, checked on every request |
| Session linked to RefreshToken | Revoking a session also revokes the token (no orphaned sessions) |
| Inactivity timeout | Automatic cleanup prevents stale sessions from accumulating |
| Audit logging on every status change | Full traceability for compliance |
| Email notifications on status change | Users are informed when their access changes |


---

## RequestDocuments Command

In addition to Approve and Reject, the CRM Manager can request additional documents
from the user before making a decision:

```csharp
// Pattern for RequestDocumentsCommand (in Features/Vetting/Commands/RequestDocuments/)
public sealed record RequestDocumentsCommand(
    string UserId,
    string Reason,
    List<string> RequiredDocuments) : IRequest<ApiResponse<string>>;

// Handler pattern:
public async Task<ApiResponse<string>> Handle(
    RequestDocumentsCommand request, CancellationToken ct)
{
    var user = await _identityService.GetUserByIdAsync(request.UserId, ct);
    if (user is null) return ApiResponse<string>.Fail("User not found.");

    if (user.AccountStatus is not AccountStatus.PendingApproval)
        return ApiResponse<string>.Fail("Can only request documents from pending users.");

    user.AccountStatus = AccountStatus.DocumentsRequested;
    user.AccountStatusReason = request.Reason;
    user.AccountStatusChangedAtUtc = DateTime.UtcNow;
    user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

    await _identityService.SaveUserAsync(user, ct);

    // Notify user about required documents
    await _emailService.SendDocumentsRequestedAsync(
        user.Email!, user.FullName, request.RequiredDocuments, ct);

    return ApiResponse<string>.Ok(request.UserId, "Documents requested.");
}
```

### The DocumentsRequested Status

When a user is in `DocumentsRequested` status:
- They CAN still log in
- They CAN still browse the platform
- They CAN create loans and products (unlike PendingApproval)
- The `AccountStatusBehaviour` treats it like `Active`
- They see a banner prompting them to upload documents

This status exists because the CRM might need additional KYC (Know Your Customer)
documents before final approval, but the user shouldn't be completely locked out.

---

## How Login Enforces Account Status

The `LoginCommandHandler` checks account status BEFORE issuing tokens:

```csharp
// From LoginCommandHandler
private static string? CheckAccountStatus(AccountStatus status)
{
    return status switch
    {
        AccountStatus.Suspended => "Your account has been suspended. Please contact support.",
        AccountStatus.Closed => "Your account has been permanently closed.",
        AccountStatus.PendingApproval => "Your account is pending approval.",
        _ => null  // Active, Hold, Blocked, DocumentsRequested → allow login
    };
}
```

**Important distinction:**
- `PendingApproval` → Cannot login at all (no tokens issued)
- `Hold` → CAN login, but `AccountStatusBehaviour` blocks new loans/products
- `Blocked` → CAN login, but specific activities are blocked
- `Suspended` → Cannot login
- `Closed` → Cannot login

This means:
- A user on `Hold` can still view their existing loans and make payments
- A `Blocked` user can still do everything except the blocked activity
- `Suspended` and `Closed` are complete lockouts at the authentication level

---

## Extending the System: Adding a New Status

If you need to add a new account status (e.g., `UnderReview`):

### Step 1: Add to the enum

```csharp
public enum AccountStatus
{
    PendingApproval,
    Active,
    Hold,
    Blocked,
    Suspended,
    Closed,
    DocumentsRequested,
    UnderReview  // ← New status
}
```

### Step 2: Define behavior in AccountStatusBehaviour

```csharp
case AccountStatus.UnderReview:
    // Example: allow viewing but block all write operations
    if (request is IWriteCommand)
    {
        throw new AccountStatusException(
            AccountStatus.UnderReview,
            "AUTH_UNDER_REVIEW",
            "Your account is under review. Write operations are temporarily disabled.");
    }
    break;
```

### Step 3: Decide login behavior

```csharp
private static string? CheckAccountStatus(AccountStatus status)
{
    return status switch
    {
        AccountStatus.Suspended => AccountSuspendedError,
        AccountStatus.Closed => AccountClosedError,
        AccountStatus.PendingApproval => AccountPendingError,
        // AccountStatus.UnderReview → allow login (handled by pipeline)
        _ => null
    };
}
```

### Step 4: Add migration

```bash
dotnet ef migrations add AddUnderReviewStatus --context AuthIdentityDbContext
```

---

## Testing Account Lifecycle

### Unit Test: Approve Registration

```csharp
[Fact]
public async Task ApproveRegistration_SetsCreditTierForBorrower()
{
    // Arrange
    var user = new ApplicationUser
    {
        Id = "user-1",
        Email = "borrower@test.com",
        AccountStatus = AccountStatus.PendingApproval
    };

    _mockIdentityService.Setup(x => x.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
        .ReturnsAsync(user);
    _mockIdentityService.Setup(x => x.GetUserRolesAsync("user-1", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<string> { "Borrower" }.AsReadOnly());
    _mockIdentityService.Setup(x => x.SaveUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var command = new ApproveRegistrationCommand(
        UserId: "user-1",
        Reason: "Documents verified",
        CreditTier: CreditTier.Silver,
        CreditLimit: 50000m);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    Assert.Equal(AccountStatus.Active, user.AccountStatus);
    Assert.Equal(CreditTier.Silver, user.CreditTier);
    Assert.Equal(50000m, user.CreditLimit);
}
```

### Unit Test: AccountStatusBehaviour Blocks Pending Users

```csharp
[Fact]
public async Task PendingUser_BlockedFromCreatingLoan()
{
    // Arrange
    _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
    _mockCurrentUser.Setup(x => x.UserId).Returns("user-1");
    _mockIdentityService.Setup(x => x.GetUserByIdAsync("user-1", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ApplicationUser { AccountStatus = AccountStatus.PendingApproval });

    var behaviour = new AccountStatusBehaviour<CreateLoanCommand, ApiResponse<string>>(
        _mockCurrentUser.Object, _mockIdentityService.Object);

    var command = new CreateLoanCommand(/* ... */);

    // Act & Assert
    await Assert.ThrowsAsync<AccountStatusException>(() =>
        behaviour.Handle(command, () => Task.FromResult(ApiResponse<string>.Ok("ok")), CancellationToken.None));
}

[Fact]
public async Task PendingUser_AllowedToViewProfile()
{
    // Arrange (same setup as above)
    var behaviour = new AccountStatusBehaviour<GetMyProfileQuery, ApiResponse<UserProfileDto>>(
        _mockCurrentUser.Object, _mockIdentityService.Object);

    // GetMyProfileQuery implements IAllowPendingApproval
    var query = new GetMyProfileQuery();

    // Act — should NOT throw
    var result = await behaviour.Handle(query,
        () => Task.FromResult(ApiResponse<UserProfileDto>.Ok(new UserProfileDto())),
        CancellationToken.None);

    // Assert
    Assert.True(result.Success);
}
```

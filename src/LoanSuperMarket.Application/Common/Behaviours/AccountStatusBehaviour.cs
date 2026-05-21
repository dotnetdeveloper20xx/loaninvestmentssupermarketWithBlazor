using LoanSuperMarket.Application.Common.Exceptions;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that enforces account status restrictions.
/// <list type="bullet">
///   <item>PendingApproval: only requests implementing IAllowPendingApproval are permitted.</item>
///   <item>Hold: blocks ICreateLoanCommand and ICreateProductCommand; all other operations allowed.</item>
///   <item>Blocked: blocks operations based on the user's BlockedActivity claim (Borrowing, Lending, or Both).</item>
///   <item>Suspended: denies all access.</item>
///   <item>Closed: denies all access (authentication should already be blocked).</item>
/// </list>
/// </summary>
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
        // Skip enforcement for unauthenticated requests (e.g., login, register)
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await next(cancellationToken);
        }

        var user = await _identityService.GetUserByIdAsync(_currentUserService.UserId, cancellationToken);

        // If user not found, allow the request to proceed (other middleware will handle auth)
        if (user is null)
        {
            return await next(cancellationToken);
        }

        switch (user.AccountStatus)
        {
            case AccountStatus.Closed:
                throw new AccountStatusException(
                    AccountStatus.Closed,
                    "AUTH_ACCOUNT_CLOSED",
                    "This account has been permanently closed. All access is denied.");

            case AccountStatus.Suspended:
                throw new AccountStatusException(
                    AccountStatus.Suspended,
                    "AUTH_ACCOUNT_SUSPENDED",
                    "This account has been suspended. All platform access is denied.");

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
                // No restrictions for Active or DocumentsRequested statuses
                break;
        }

        return await next(cancellationToken);
    }

    /// <summary>
    /// PendingApproval users can only execute requests that implement IAllowPendingApproval
    /// (typically profile-viewing queries).
    /// </summary>
    private static void EnforcePendingApproval(TRequest request)
    {
        if (request is IAllowPendingApproval)
        {
            return;
        }

        throw new AccountStatusException(
            AccountStatus.PendingApproval,
            "AUTH_PENDING_APPROVAL",
            "Your account is pending approval. You can only view your profile until your account is approved.");
    }

    /// <summary>
    /// Hold users cannot create new loan applications or new loan products.
    /// Existing loans and lending activities are allowed to continue.
    /// </summary>
    private static void EnforceHold(TRequest request)
    {
        if (request is ICreateLoanCommand)
        {
            throw new AccountStatusException(
                AccountStatus.Hold,
                "AUTH_ACCOUNT_HOLD",
                "Your account is on hold. You cannot create new loan applications.");
        }

        if (request is ICreateProductCommand)
        {
            throw new AccountStatusException(
                AccountStatus.Hold,
                "AUTH_ACCOUNT_HOLD",
                "Your account is on hold. You cannot create new loan products.");
        }
    }

    /// <summary>
    /// Blocked users cannot perform the specific blocked activity configured by the Admin.
    /// BlockedActivity can be "Borrowing", "Lending", or "Both".
    /// </summary>
    private static void EnforceBlocked(TRequest request, string? blockedActivity)
    {
        if (string.IsNullOrEmpty(blockedActivity))
        {
            return;
        }

        var isBorrowingBlocked = blockedActivity.Equals("Borrowing", StringComparison.OrdinalIgnoreCase)
                                 || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);

        var isLendingBlocked = blockedActivity.Equals("Lending", StringComparison.OrdinalIgnoreCase)
                               || blockedActivity.Equals("Both", StringComparison.OrdinalIgnoreCase);

        if (isBorrowingBlocked && request is ICreateLoanCommand)
        {
            throw new AccountStatusException(
                AccountStatus.Blocked,
                "AUTH_ACCOUNT_BLOCKED",
                "Your account is blocked from borrowing activities. You cannot create new loan applications.");
        }

        if (isLendingBlocked && request is ICreateProductCommand)
        {
            throw new AccountStatusException(
                AccountStatus.Blocked,
                "AUTH_ACCOUNT_BLOCKED",
                "Your account is blocked from lending activities. You cannot create new loan products.");
        }
    }
}

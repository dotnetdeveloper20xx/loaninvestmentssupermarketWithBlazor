using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user's account status prevents them from performing an operation.
/// </summary>
public sealed class AccountStatusException : Exception
{
    public AccountStatusException(AccountStatus status, string errorCode, string message)
        : base(message)
    {
        Status = status;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// The account status that caused the restriction.
    /// </summary>
    public AccountStatus Status { get; }

    /// <summary>
    /// A machine-readable error code (e.g., AUTH_PENDING_APPROVAL, AUTH_ACCOUNT_SUSPENDED, AUTH_ACCOUNT_CLOSED).
    /// </summary>
    public string ErrorCode { get; }
}

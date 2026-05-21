namespace LoanSuperMarket.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user's operation exceeds a configured limit
/// (credit limit, capital limit, or maximum active loans).
/// </summary>
public sealed class LimitExceededException : Exception
{
    public LimitExceededException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// A machine-readable error code identifying the type of limit exceeded.
    /// Possible values: LIMIT_CREDIT_EXCEEDED, LIMIT_CAPITAL_EXCEEDED, LIMIT_MAX_LOANS.
    /// </summary>
    public string ErrorCode { get; }
}

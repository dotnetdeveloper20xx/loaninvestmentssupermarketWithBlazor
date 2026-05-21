namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that create or submit loan applications.
/// Used by LimitEnforcementBehaviour to enforce borrower credit limits
/// and maximum active loans restrictions.
/// </summary>
public interface ILoanApplicationCommand
{
    /// <summary>
    /// The requested loan amount for the application.
    /// </summary>
    decimal Amount { get; }
}

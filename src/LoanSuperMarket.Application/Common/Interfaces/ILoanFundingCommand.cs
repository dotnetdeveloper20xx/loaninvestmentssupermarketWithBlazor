namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that fund loan products.
/// Used by LimitEnforcementBehaviour to enforce lender capital limits.
/// </summary>
public interface ILoanFundingCommand
{
    /// <summary>
    /// The funding amount for the loan product.
    /// </summary>
    decimal Amount { get; }
}

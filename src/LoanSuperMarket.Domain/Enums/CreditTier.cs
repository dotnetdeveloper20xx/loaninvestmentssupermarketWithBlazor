namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Credit tier classification for borrowers determining interest rate ranges and loan limits.
/// </summary>
public enum CreditTier
{
    /// <summary>Excellent: 10-11% interest, high limits</summary>
    A,

    /// <summary>Good: 12-13% interest, medium limits</summary>
    B,

    /// <summary>Fair: 14-15% interest, lower limits</summary>
    C
}

namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Represents the overall performance classification of a funded loan.
/// </summary>
public enum LoanPerformance
{
    /// <summary>All payments are on time.</summary>
    OnTime = 1,

    /// <summary>One or more payments are late.</summary>
    Late = 2,

    /// <summary>Three or more consecutive payments missed — loan is in default.</summary>
    Defaulted = 3
}

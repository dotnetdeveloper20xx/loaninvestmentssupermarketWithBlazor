namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Status of a defaulted loan in the collections process.
/// </summary>
public enum CollectionStatus
{
    /// <summary>Loan just entered default, not yet actioned.</summary>
    New = 1,

    /// <summary>First contact attempt made to borrower.</summary>
    ContactAttempted = 2,

    /// <summary>Payment plan agreed with borrower.</summary>
    PaymentPlanAgreed = 3,

    /// <summary>Borrower is making payments under the agreed plan.</summary>
    InRepaymentPlan = 4,

    /// <summary>Loan written off as unrecoverable.</summary>
    WrittenOff = 5,

    /// <summary>Full recovery achieved.</summary>
    Recovered = 6
}

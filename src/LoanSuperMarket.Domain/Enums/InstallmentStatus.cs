namespace LoanSuperMarket.Domain.Enums;

/// <summary>
/// Represents the payment status of a loan installment.
/// </summary>
public enum InstallmentStatus
{
    /// <summary>Payment not yet due or awaiting payment.</summary>
    Pending = 1,

    /// <summary>Full payment received.</summary>
    Paid = 2,

    /// <summary>Partial payment received, balance remaining.</summary>
    PartiallyPaid = 3,

    /// <summary>Payment is overdue past the grace period.</summary>
    Late = 4,

    /// <summary>Payment was not made and the next installment is now due.</summary>
    Missed = 5
}

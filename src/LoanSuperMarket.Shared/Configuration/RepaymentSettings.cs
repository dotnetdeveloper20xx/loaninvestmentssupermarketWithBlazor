namespace LoanSuperMarket.Shared.Configuration;

/// <summary>
/// Configuration settings for the repayment engine including grace periods,
/// late fee percentages, and notification thresholds.
/// </summary>
public sealed class RepaymentSettings
{
    /// <summary>
    /// Number of days after due date before an installment is marked late.
    /// </summary>
    public int GracePeriodDays { get; set; } = 5;

    /// <summary>
    /// Percentage of outstanding amount charged as a late fee (e.g. 0.02 = 2%).
    /// </summary>
    public decimal LateFeePercentage { get; set; } = 0.02m;

    /// <summary>
    /// Number of consecutive missed/late installments before a loan is considered defaulted.
    /// </summary>
    public int ConsecutiveMissedForDefault { get; set; } = 3;

    /// <summary>
    /// Number of days before due date to send an upcoming payment reminder.
    /// </summary>
    public int UpcomingPaymentReminderDays { get; set; } = 3;
}

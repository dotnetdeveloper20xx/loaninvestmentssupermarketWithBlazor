namespace LoanSuperMarket.Shared.Dashboard;

public sealed class BorrowerLoanDto
{
    public Guid ScheduleId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public decimal FundedAmount { get; set; }

    public int TermMonths { get; set; }

    public decimal EffectiveRate { get; set; }

    public DateTime? NextDueDate { get; set; }

    public decimal? NextAmount { get; set; }

    public int PaidCount { get; set; }

    public int TotalCount { get; set; }

    public decimal ProgressPercentage { get; set; }

    public bool IsDueSoon { get; set; }

    public bool HasLateOrMissed { get; set; }
}

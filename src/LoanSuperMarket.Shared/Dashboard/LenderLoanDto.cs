namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderLoanDto
{
    public Guid ScheduleId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public decimal FundedAmount { get; set; }

    public int TermMonths { get; set; }

    public decimal EffectiveRate { get; set; }

    public string Performance { get; set; } = string.Empty;

    public DateTime? NextDueDate { get; set; }
}

namespace LoanSuperMarket.Shared.Funding;

public sealed class FundingResultDto
{
    public Guid ScheduleId { get; set; }

    public decimal MonthlyEmi { get; set; }

    public decimal TotalInterest { get; set; }

    public int TermMonths { get; set; }

    public decimal FundedAmount { get; set; }

    public decimal EffectiveRate { get; set; }
}

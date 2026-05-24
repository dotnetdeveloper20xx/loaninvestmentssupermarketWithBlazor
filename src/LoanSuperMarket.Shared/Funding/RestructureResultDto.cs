namespace LoanSuperMarket.Shared.Funding;

public sealed class RestructureResultDto
{
    public Guid ScheduleId { get; set; }

    public decimal NewRate { get; set; }

    public int NewTermMonths { get; set; }

    public decimal NewMonthlyEmi { get; set; }

    public decimal NewTotalInterest { get; set; }

    public int RemainingInstallments { get; set; }
}

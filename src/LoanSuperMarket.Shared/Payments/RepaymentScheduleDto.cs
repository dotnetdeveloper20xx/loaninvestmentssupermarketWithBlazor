namespace LoanSuperMarket.Shared.Payments;

public sealed class RepaymentScheduleDto
{
    public Guid ScheduleId { get; set; }

    public Guid LoanApplicationId { get; set; }

    public decimal FundedAmount { get; set; }

    public decimal AnnualInterestRate { get; set; }

    public int TermMonths { get; set; }

    public decimal MonthlyEmi { get; set; }

    public decimal TotalInterestPayable { get; set; }

    public string Performance { get; set; } = string.Empty;

    public DateTime GeneratedAtUtc { get; set; }

    public List<InstallmentDto> Installments { get; set; } = [];
}

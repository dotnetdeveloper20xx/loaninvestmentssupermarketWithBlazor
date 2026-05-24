namespace LoanSuperMarket.Shared.Dashboard;

public sealed class AdminLoansOverviewDto
{
    public int TotalActiveLoans { get; set; }

    public int TotalDefaultedLoans { get; set; }

    public decimal TotalOutstandingPrincipal { get; set; }

    public decimal TotalFundedAllTime { get; set; }

    public decimal PlatformDefaultRate { get; set; }

    public List<AdminLoanItemDto> Loans { get; set; } = [];
}

public sealed class AdminLoanItemDto
{
    public Guid ScheduleId { get; set; }

    public string LenderName { get; set; } = string.Empty;

    public string BorrowerName { get; set; } = string.Empty;

    public decimal FundedAmount { get; set; }

    public decimal EffectiveRate { get; set; }

    public int TermMonths { get; set; }

    public string Performance { get; set; } = string.Empty;

    public DateTime FundedDate { get; set; }

    public int PaidInstallments { get; set; }

    public int TotalInstallments { get; set; }
}

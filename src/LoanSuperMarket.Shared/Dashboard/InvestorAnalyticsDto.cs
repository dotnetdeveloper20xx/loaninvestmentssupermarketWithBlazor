namespace LoanSuperMarket.Shared.Dashboard;

public sealed class InvestorAnalyticsDto
{
    public decimal TotalInvested { get; set; }

    public decimal TotalReturned { get; set; }

    public decimal NetProfit { get; set; }

    public decimal AnnualizedYield { get; set; }

    public decimal AverageRoi { get; set; }

    public int TotalLoans { get; set; }

    public int PerformingLoans { get; set; }

    public int LateLoans { get; set; }

    public int DefaultedLoans { get; set; }

    public decimal DiversificationScore { get; set; }

    public List<LoanRoiDto> LoanBreakdown { get; set; } = [];
}

public sealed class LoanRoiDto
{
    public Guid ScheduleId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public decimal FundedAmount { get; set; }

    public decimal InterestEarned { get; set; }

    public decimal LateFeesEarned { get; set; }

    public decimal TotalReturn { get; set; }

    public decimal RoiPercentage { get; set; }

    public string Performance { get; set; } = string.Empty;
}

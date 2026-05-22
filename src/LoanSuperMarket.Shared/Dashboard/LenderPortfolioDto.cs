namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderPortfolioDto
{
    public decimal TotalFunded { get; set; }

    public int ActiveLoanCount { get; set; }

    public decimal OutstandingPrincipal { get; set; }

    public decimal ExpectedMonthlyIncome { get; set; }

    public decimal DefaultRate { get; set; }

    public decimal AvailableFunds { get; set; }
}

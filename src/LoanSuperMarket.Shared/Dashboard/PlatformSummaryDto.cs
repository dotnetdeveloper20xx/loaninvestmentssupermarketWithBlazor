namespace LoanSuperMarket.Shared.Dashboard;

public sealed class PlatformSummaryDto
{
    public int ActiveLoans { get; set; }
    public int DefaultedLoans { get; set; }
    public decimal TotalFunded { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalInterestCollected { get; set; }
    public decimal TotalLateFeesCollected { get; set; }
    public int ActiveLenders { get; set; }
    public int ActiveBorrowers { get; set; }
    public decimal TotalAvailableCapital { get; set; }
}

public sealed class MonthlyInterestReportDto
{
    public Guid LenderId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int PaidInstallments { get; set; }
    public decimal TotalInterestIncome { get; set; }
    public decimal TotalPrincipalReturned { get; set; }
    public decimal TotalLateFees { get; set; }
    public decimal TotalIncome { get; set; }
}

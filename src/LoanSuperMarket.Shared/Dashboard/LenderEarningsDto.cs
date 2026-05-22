namespace LoanSuperMarket.Shared.Dashboard;

public sealed class LenderEarningsDto
{
    public decimal TotalInterestReceived { get; set; }

    public decimal ProjectedTotalReturns { get; set; }

    public decimal TotalLateFeesCollected { get; set; }

    public decimal AvailableFunds { get; set; }
}

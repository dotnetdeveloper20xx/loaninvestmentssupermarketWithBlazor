namespace LoanSuperMarket.Shared.Dashboard;

public sealed class DashboardSummaryDto
{
    public int TotalBorrowers { get; set; }

    public int TotalLenders { get; set; }

    public int TotalLoanProducts { get; set; }

    public int TotalApplications { get; set; }

    public int ApprovedApplications { get; set; }

    public int FundedApplications { get; set; }

    public decimal TotalFundingVolume { get; set; }

    public decimal ApprovalRate { get; set; }

    public decimal FundingRate { get; set; }

    public List<RecentApplicationDto> RecentApplications { get; set; } = [];

    public List<RecentBorrowerDto> RecentBorrowers { get; set; } = [];
}
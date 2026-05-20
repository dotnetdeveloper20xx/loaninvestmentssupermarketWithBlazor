namespace LoanSuperMarket.Shared.Dashboard;

public sealed class RecentApplicationDto
{
    public Guid Id { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; }
}
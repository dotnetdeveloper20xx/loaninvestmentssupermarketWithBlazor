namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class LoanApplicationDto
{
    public Guid Id { get; set; }

    public Guid BorrowerId { get; set; }

    public Guid LoanProductId { get; set; }

    public decimal RequestedAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public int TermMonths { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
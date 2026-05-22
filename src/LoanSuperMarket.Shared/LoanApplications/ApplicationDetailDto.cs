namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class ApplicationDetailDto
{
    public Guid ApplicationId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string BorrowerEmail { get; set; } = string.Empty;

    public int CreditTier { get; set; }

    public string? ProductTitle { get; set; }

    public decimal RequestedAmount { get; set; }

    public int TermMonths { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public int Status { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public string? ReviewedBy { get; set; }

    public string? ReviewReason { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? DocumentRequestNote { get; set; }

    public List<ApplicationDocumentDto> Documents { get; set; } = [];
}

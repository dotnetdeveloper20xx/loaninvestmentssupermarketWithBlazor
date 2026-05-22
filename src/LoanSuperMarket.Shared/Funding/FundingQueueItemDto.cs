namespace LoanSuperMarket.Shared.Funding;

public sealed class FundingQueueItemDto
{
    public Guid ApplicationId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string CreditTier { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int TermMonths { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public decimal EffectiveRate { get; set; }

    public DateTime ApprovalDate { get; set; }
}

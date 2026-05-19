namespace LoanSuperMarket.Shared.LoanProducts;

public sealed class LoanProductDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal MinimumAmount { get; set; }

    public decimal MaximumAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public decimal InterestRate { get; set; }

    public int MinimumTermMonths { get; set; }

    public int MaximumTermMonths { get; set; }

    public Guid LenderId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
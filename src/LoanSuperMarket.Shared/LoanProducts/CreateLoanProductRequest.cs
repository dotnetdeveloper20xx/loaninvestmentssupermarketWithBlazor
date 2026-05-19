namespace LoanSuperMarket.Shared.LoanProducts;

public sealed class CreateLoanProductRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal MinimumAmount { get; set; }

    public decimal MaximumAmount { get; set; }

    public decimal InterestRate { get; set; }

    public int MinimumTermMonths { get; set; }

    public int MaximumTermMonths { get; set; }

    public Guid LenderId { get; set; }
}
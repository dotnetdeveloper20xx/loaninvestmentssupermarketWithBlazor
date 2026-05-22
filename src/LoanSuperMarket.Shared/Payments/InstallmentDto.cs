namespace LoanSuperMarket.Shared.Payments;

public sealed class InstallmentDto
{
    public Guid Id { get; set; }

    public int InstallmentNumber { get; set; }

    public DateTime DueDate { get; set; }

    public decimal PrincipalPortion { get; set; }

    public decimal InterestPortion { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal RemainingBalance { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }

    public DateTime? PaidDate { get; set; }

    public decimal LateFeeAmount { get; set; }

    public string? Notes { get; set; }
}

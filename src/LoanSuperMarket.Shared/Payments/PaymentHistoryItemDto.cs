namespace LoanSuperMarket.Shared.Payments;

public sealed class PaymentHistoryItemDto
{
    public int InstallmentNumber { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public decimal PaidAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal LateFeeAmount { get; set; }
}

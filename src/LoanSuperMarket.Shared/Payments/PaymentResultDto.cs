namespace LoanSuperMarket.Shared.Payments;

public sealed class PaymentResultDto
{
    public int InstallmentNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal PaidAmount { get; set; }

    public decimal RemainingOnInstallment { get; set; }

    public decimal TotalPaidToDate { get; set; }
}

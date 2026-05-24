namespace LoanSuperMarket.Shared.Payments;

public sealed class BulkPaymentResultDto
{
    public int InstallmentsPaid { get; set; }

    public decimal TotalAmountApplied { get; set; }

    public decimal RemainingOnSchedule { get; set; }

    public decimal TotalPaidToDate { get; set; }

    public bool IsFullyPaidOff { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Payments;

public sealed class RecordPaymentRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Funding;

public sealed class TopUpFundsRequest
{
    [Range(0.01, 10000000, ErrorMessage = "Amount must be between £0.01 and £10,000,000.")]
    public decimal Amount { get; set; }
}

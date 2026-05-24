using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Funding;

public sealed class RestructureLoanRequest
{
    [Range(0.01, 100, ErrorMessage = "New rate must be between 0.01% and 100%.")]
    public decimal NewAnnualRate { get; set; }

    [Range(1, 360, ErrorMessage = "New term must be between 1 and 360 months.")]
    public int NewTermMonths { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

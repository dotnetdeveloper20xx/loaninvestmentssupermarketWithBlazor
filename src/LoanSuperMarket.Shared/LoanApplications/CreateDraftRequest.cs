using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class CreateDraftRequest
{
    [Range(0.01, 999999999, ErrorMessage = "Requested amount must be greater than zero.")]
    public decimal RequestedAmount { get; set; }

    [Range(1, 600, ErrorMessage = "Term must be between 1 and 600 months.")]
    public int TermMonths { get; set; }

    [Required(ErrorMessage = "Purpose is required.")]
    [StringLength(1000, ErrorMessage = "Purpose cannot exceed 1000 characters.")]
    public string Purpose { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class CreateLoanApplicationRequest
{
    [Required(ErrorMessage = "Borrower ID is required.")]
    public Guid BorrowerId { get; set; }

    [Required(ErrorMessage = "Loan product ID is required.")]
    public Guid LoanProductId { get; set; }

    [Range(1, 999999999, ErrorMessage = "Requested amount must be greater than zero.")]
    public decimal RequestedAmount { get; set; } = 5000;

    [Range(1, 600, ErrorMessage = "Term must be between 1 and 600 months.")]
    public int TermMonths { get; set; } = 24;

    [Required(ErrorMessage = "Purpose is required.")]
    [StringLength(1000, ErrorMessage = "Purpose cannot exceed 1000 characters.")]
    public string Purpose { get; set; } = string.Empty;
}
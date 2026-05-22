using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class SelectProductRequest
{
    [Required(ErrorMessage = "Loan product ID is required.")]
    public Guid LoanProductId { get; set; }
}

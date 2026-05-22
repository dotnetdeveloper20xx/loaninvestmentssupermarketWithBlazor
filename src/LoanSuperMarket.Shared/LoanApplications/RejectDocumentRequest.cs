using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class RejectDocumentRequest
{
    [Required(ErrorMessage = "Rejection note is required.")]
    [StringLength(2000, ErrorMessage = "Rejection note cannot exceed 2000 characters.")]
    public string RejectionNote { get; set; } = string.Empty;
}

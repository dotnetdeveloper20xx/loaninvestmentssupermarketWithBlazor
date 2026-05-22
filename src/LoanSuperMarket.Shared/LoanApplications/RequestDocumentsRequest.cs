using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class RequestDocumentsRequest
{
    [Required(ErrorMessage = "Note is required.")]
    [StringLength(2000, ErrorMessage = "Note cannot exceed 2000 characters.")]
    public string Note { get; set; } = string.Empty;
}

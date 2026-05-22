using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanApplications;

public sealed class ApproveRejectRequest
{
    [Required(ErrorMessage = "Reason is required.")]
    [StringLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters.")]
    public string Reason { get; set; } = string.Empty;
}

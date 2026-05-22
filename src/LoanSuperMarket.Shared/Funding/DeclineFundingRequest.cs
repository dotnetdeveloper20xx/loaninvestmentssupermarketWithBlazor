using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Funding;

public sealed class DeclineFundingRequest
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required(ErrorMessage = "A decline reason is required.")]
    [StringLength(1000, ErrorMessage = "Decline reason cannot exceed 1000 characters.")]
    public string Reason { get; set; } = string.Empty;
}

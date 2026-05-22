using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Funding;

public sealed class AcceptFundingRequest
{
    [Required]
    public Guid ApplicationId { get; set; }
}

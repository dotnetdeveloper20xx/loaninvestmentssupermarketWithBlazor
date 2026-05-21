using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Credit.Commands.SetCreditTier;

/// <summary>
/// Command to update a borrower's credit tier. Requires justification for audit purposes.
/// </summary>
public sealed record SetCreditTierCommand(
    string BorrowerUserId,
    CreditTier Tier,
    string Justification) : IRequest<ApiResponse<string>>;

using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Credit.Commands.SetCreditLimit;

/// <summary>
/// Command to update a borrower's credit limit. Requires justification for audit purposes.
/// </summary>
public sealed record SetCreditLimitCommand(
    string UserId,
    decimal Limit,
    string Justification) : IRequest<ApiResponse<string>>;

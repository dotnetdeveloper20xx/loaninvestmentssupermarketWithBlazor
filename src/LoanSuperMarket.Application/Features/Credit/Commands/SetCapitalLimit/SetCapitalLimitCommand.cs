using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Credit.Commands.SetCapitalLimit;

/// <summary>
/// Command to update a lender's capital limit. Requires justification for audit purposes.
/// </summary>
public sealed record SetCapitalLimitCommand(
    string UserId,
    decimal Limit,
    string Justification) : IRequest<ApiResponse<string>>;

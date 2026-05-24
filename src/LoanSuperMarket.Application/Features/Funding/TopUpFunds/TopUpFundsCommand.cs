using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.TopUpFunds;

public sealed record TopUpFundsCommand(
    Guid LenderId,
    decimal Amount) : IRequest<ApiResponse<decimal>>;

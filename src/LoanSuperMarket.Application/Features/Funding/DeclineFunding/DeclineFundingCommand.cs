using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.DeclineFunding;

public sealed record DeclineFundingCommand(
    Guid ApplicationId,
    Guid LenderId,
    string DeclineReason) : IRequest<ApiResponse<string>>;

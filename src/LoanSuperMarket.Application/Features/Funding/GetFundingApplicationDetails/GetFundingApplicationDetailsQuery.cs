using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.GetFundingApplicationDetails;

public sealed record GetFundingApplicationDetailsQuery(Guid ApplicationId)
    : IRequest<ApiResponse<FundingApplicationDetailDto>>;

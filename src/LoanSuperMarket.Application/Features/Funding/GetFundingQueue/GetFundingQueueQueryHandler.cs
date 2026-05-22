using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.GetFundingQueue;

public sealed class GetFundingQueueQueryHandler
    : IRequestHandler<GetFundingQueueQuery, ApiResponse<IReadOnlyList<FundingQueueItemDto>>>
{
    private readonly ILoanApplicationRepository _loanApplicationRepository;

    public GetFundingQueueQueryHandler(ILoanApplicationRepository loanApplicationRepository)
    {
        _loanApplicationRepository = loanApplicationRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<FundingQueueItemDto>>> Handle(
        GetFundingQueueQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _loanApplicationRepository.GetFundingQueueAsync(
            request.FilterByUserId,
            request.ProductTitleFilter,
            request.MinAmount,
            request.MaxAmount,
            cancellationToken);

        return ApiResponse<IReadOnlyList<FundingQueueItemDto>>.Ok(
            items,
            "Funding queue retrieved successfully.");
    }
}

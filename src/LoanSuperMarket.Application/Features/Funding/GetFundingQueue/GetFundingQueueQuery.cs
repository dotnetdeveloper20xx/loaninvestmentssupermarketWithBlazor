using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.GetFundingQueue;

public sealed class GetFundingQueueQuery : IRequest<ApiResponse<IReadOnlyList<FundingQueueItemDto>>>, IResourceFilteredQuery
{
    public string? ProductTitleFilter { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}

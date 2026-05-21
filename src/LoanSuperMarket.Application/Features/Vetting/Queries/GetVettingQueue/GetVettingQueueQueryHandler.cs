using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Queries.GetVettingQueue;

/// <summary>
/// Handles the GetVettingQueueQuery by retrieving PendingApproval users sorted by registration date.
/// </summary>
public sealed class GetVettingQueueQueryHandler
    : IRequestHandler<GetVettingQueueQuery, PagedResult<VettingItemDto>>
{
    private readonly IUserQueryService _userQueryService;

    public GetVettingQueueQueryHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<PagedResult<VettingItemDto>> Handle(
        GetVettingQueueQuery request,
        CancellationToken cancellationToken)
    {
        return await _userQueryService.GetVettingQueueAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}

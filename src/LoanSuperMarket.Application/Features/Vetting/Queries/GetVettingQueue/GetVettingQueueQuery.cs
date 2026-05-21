using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Queries.GetVettingQueue;

/// <summary>
/// Query to retrieve the vetting queue of users pending approval, sorted by registration date.
/// </summary>
public sealed record GetVettingQueueQuery(
    int Page,
    int PageSize) : IRequest<PagedResult<VettingItemDto>>;

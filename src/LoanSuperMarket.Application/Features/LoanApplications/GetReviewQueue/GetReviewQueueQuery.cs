using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetReviewQueue;

public sealed record GetReviewQueueQuery(
    int? StatusFilter = null,
    string? SortBy = null) : IRequest<IReadOnlyList<ReviewQueueItemDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}

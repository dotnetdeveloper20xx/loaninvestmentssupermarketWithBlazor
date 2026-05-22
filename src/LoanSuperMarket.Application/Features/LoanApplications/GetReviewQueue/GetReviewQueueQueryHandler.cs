using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetReviewQueue;

public sealed class GetReviewQueueQueryHandler
    : IRequestHandler<GetReviewQueueQuery, IReadOnlyList<ReviewQueueItemDto>>
{
    private readonly ILoanApplicationRepository _applicationRepository;

    public GetReviewQueueQueryHandler(ILoanApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<IReadOnlyList<ReviewQueueItemDto>> Handle(
        GetReviewQueueQuery request,
        CancellationToken cancellationToken)
    {
        LoanApplicationStatus[]? statusFilter = null;

        if (request.StatusFilter.HasValue &&
            Enum.IsDefined(typeof(LoanApplicationStatus), request.StatusFilter.Value))
        {
            statusFilter = [(LoanApplicationStatus)request.StatusFilter.Value];
        }
        else
        {
            // Default: show Submitted, UnderReview, DocumentsRequested
            statusFilter =
            [
                LoanApplicationStatus.Submitted,
                LoanApplicationStatus.UnderReview,
                LoanApplicationStatus.DocumentsRequested
            ];
        }

        return await _applicationRepository.GetReviewQueueAsync(
            statusFilter, request.SortBy, cancellationToken);
    }
}

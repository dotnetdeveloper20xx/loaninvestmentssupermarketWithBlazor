using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetBorrowerApplications;

public sealed class GetBorrowerApplicationsQueryHandler
    : IRequestHandler<GetBorrowerApplicationsQuery, IReadOnlyList<WizardApplicationSummaryDto>>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetBorrowerApplicationsQueryHandler(
        ILoanApplicationRepository applicationRepository,
        IBorrowerRepository borrowerRepository,
        ICurrentUserService currentUserService)
    {
        _applicationRepository = applicationRepository;
        _borrowerRepository = borrowerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<WizardApplicationSummaryDto>> Handle(
        GetBorrowerApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.FilterByUserId ?? _currentUserService.UserId
            ?? throw new DomainException("User is not authenticated.");

        var borrower = await _borrowerRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Borrower profile was not found.");

        return await _applicationRepository.GetByBorrowerIdAsync(
            borrower.Id, cancellationToken);
    }
}

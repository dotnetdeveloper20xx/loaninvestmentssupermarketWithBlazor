using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetLoanApplications;

public sealed class GetLoanApplicationsQueryHandler
    : IRequestHandler<GetLoanApplicationsQuery, IReadOnlyList<LoanApplicationDto>>
{
    private readonly ILoanApplicationRepository _applicationRepository;

    public GetLoanApplicationsQueryHandler(ILoanApplicationRepository applicationRepository)
    {
        _applicationRepository = applicationRepository;
    }

    public async Task<IReadOnlyList<LoanApplicationDto>> Handle(
        GetLoanApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);

        return applications
            .Select(x => new LoanApplicationDto
            {
                Id = x.Id,
                BorrowerId = x.BorrowerId,
                LoanProductId = x.LoanProductId ?? Guid.Empty,
                RequestedAmount = x.RequestedAmount.Amount,
                Currency = x.RequestedAmount.Currency,
                TermMonths = x.TermMonths,
                Purpose = x.Purpose,
                Status = x.Status.ToString(),
                SubmittedAtUtc = x.SubmittedAtUtc ?? DateTime.MinValue,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}
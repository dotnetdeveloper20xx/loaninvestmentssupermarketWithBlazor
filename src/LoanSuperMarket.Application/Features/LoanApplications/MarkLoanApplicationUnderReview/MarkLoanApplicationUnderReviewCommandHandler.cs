using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.MarkLoanApplicationUnderReview;

public sealed class MarkLoanApplicationUnderReviewCommandHandler
    : IRequestHandler<MarkLoanApplicationUnderReviewCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public MarkLoanApplicationUnderReviewCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(MarkLoanApplicationUnderReviewCommand request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.MarkUnderReview();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
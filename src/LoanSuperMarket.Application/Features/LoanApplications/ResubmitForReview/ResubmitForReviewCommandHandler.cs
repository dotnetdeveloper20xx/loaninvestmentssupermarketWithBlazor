using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.ResubmitForReview;

public sealed class ResubmitForReviewCommandHandler
    : IRequestHandler<ResubmitForReviewCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public ResubmitForReviewCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        ResubmitForReviewCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        application.ResubmitForReview();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

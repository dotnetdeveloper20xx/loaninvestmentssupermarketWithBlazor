using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;

public sealed class RejectLoanApplicationCommandHandler
    : IRequestHandler<RejectLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public RejectLoanApplicationCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RejectLoanApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.Reject();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
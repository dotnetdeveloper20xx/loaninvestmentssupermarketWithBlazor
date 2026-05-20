using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.ApproveLoanApplication;

public sealed class ApproveLoanApplicationCommandHandler
    : IRequestHandler<ApproveLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public ApproveLoanApplicationCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ApproveLoanApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.Approve();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
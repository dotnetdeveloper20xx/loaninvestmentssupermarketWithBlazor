using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.FundLoanApplication;

public sealed class FundLoanApplicationCommandHandler
    : IRequestHandler<FundLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public FundLoanApplicationCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(FundLoanApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.Fund();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
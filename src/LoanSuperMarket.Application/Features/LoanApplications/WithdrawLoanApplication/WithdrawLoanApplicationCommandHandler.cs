using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.WithdrawLoanApplication;

public sealed class WithdrawLoanApplicationCommandHandler
    : IRequestHandler<WithdrawLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public WithdrawLoanApplicationCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        WithdrawLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        application.Withdraw();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

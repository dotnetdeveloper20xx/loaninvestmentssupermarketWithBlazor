using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.UpdateDraftLoanApplication;

public sealed class UpdateDraftLoanApplicationCommandHandler
    : IRequestHandler<UpdateDraftLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;

    public UpdateDraftLoanApplicationCommandHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        UpdateDraftLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        application.UpdateParameters(request.RequestedAmount, request.TermMonths, request.Purpose);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

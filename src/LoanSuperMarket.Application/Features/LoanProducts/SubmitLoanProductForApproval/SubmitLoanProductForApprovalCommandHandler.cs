using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.SubmitLoanProductForApproval;

public sealed class SubmitLoanProductForApprovalCommandHandler
    : IRequestHandler<SubmitLoanProductForApprovalCommand>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public SubmitLoanProductForApprovalCommandHandler(
        ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task Handle(
        SubmitLoanProductForApprovalCommand request,
        CancellationToken cancellationToken)
    {
        var loanProduct = await _loanProductRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (loanProduct is null)
        {
            throw new InvalidOperationException("Loan product was not found.");
        }

        loanProduct.SubmitForApproval();

        await _loanProductRepository.SaveChangesAsync(cancellationToken);
    }
}
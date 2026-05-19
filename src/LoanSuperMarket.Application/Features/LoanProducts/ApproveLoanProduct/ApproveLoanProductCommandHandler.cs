using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.ApproveLoanProduct;

public sealed class ApproveLoanProductCommandHandler
    : IRequestHandler<ApproveLoanProductCommand>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public ApproveLoanProductCommandHandler(
        ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task Handle(
        ApproveLoanProductCommand request,
        CancellationToken cancellationToken)
    {
        var loanProduct = await _loanProductRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (loanProduct is null)
        {
            throw new InvalidOperationException("Loan product was not found.");
        }

        loanProduct.Approve();

        await _loanProductRepository.SaveChangesAsync(cancellationToken);
    }
}
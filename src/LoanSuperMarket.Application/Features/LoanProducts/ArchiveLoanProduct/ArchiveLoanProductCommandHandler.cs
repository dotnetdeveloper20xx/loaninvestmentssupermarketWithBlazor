using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.ArchiveLoanProduct;

public sealed class ArchiveLoanProductCommandHandler
    : IRequestHandler<ArchiveLoanProductCommand>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public ArchiveLoanProductCommandHandler(
        ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task Handle(
        ArchiveLoanProductCommand request,
        CancellationToken cancellationToken)
    {
        var loanProduct = await _loanProductRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (loanProduct is null)
        {
            throw new InvalidOperationException("Loan product was not found.");
        }

        loanProduct.Archive();

        await _loanProductRepository.SaveChangesAsync(cancellationToken);
    }
}
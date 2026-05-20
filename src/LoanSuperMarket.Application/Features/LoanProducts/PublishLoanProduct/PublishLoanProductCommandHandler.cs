using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.PublishLoanProduct;

public sealed class PublishLoanProductCommandHandler
    : IRequestHandler<PublishLoanProductCommand>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public PublishLoanProductCommandHandler(
        ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task Handle(
        PublishLoanProductCommand request,
        CancellationToken cancellationToken)
    {
        var loanProduct = await _loanProductRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (loanProduct is null)
        {
            throw new InvalidOperationException("Loan product was not found.");
        }

        loanProduct.Publish();

        await _loanProductRepository.SaveChangesAsync(cancellationToken);
    }
}
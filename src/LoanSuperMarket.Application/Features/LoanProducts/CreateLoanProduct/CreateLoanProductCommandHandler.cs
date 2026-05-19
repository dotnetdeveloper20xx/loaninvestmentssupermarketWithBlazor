using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.ValueObjects;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed class CreateLoanProductCommandHandler
    : IRequestHandler<CreateLoanProductCommand, Guid>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public CreateLoanProductCommandHandler(ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<Guid> Handle(
        CreateLoanProductCommand request,
        CancellationToken cancellationToken)
    {
        var minimumAmount = Money.Create(request.MinimumAmount);
        var maximumAmount = Money.Create(request.MaximumAmount);
        var interestRate = InterestRate.Create(request.InterestRate);

        var loanProduct = LoanProduct.Create(
            request.Title,
            request.Description,
            minimumAmount,
            maximumAmount,
            interestRate,
            request.MinimumTermMonths,
            request.MaximumTermMonths,
            request.LenderId);

        await _loanProductRepository.AddAsync(loanProduct, cancellationToken);
        await _loanProductRepository.SaveChangesAsync(cancellationToken);

        return loanProduct.Id;
    }
}
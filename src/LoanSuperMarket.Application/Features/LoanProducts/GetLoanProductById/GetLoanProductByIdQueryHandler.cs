using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductById;

public sealed class GetLoanProductByIdQueryHandler
    : IRequestHandler<GetLoanProductByIdQuery, LoanProductDto?>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public GetLoanProductByIdQueryHandler(ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<LoanProductDto?> Handle(
        GetLoanProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _loanProductRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new LoanProductDto
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            MinimumAmount = product.MinimumAmount.Amount,
            MaximumAmount = product.MaximumAmount.Amount,
            Currency = product.MinimumAmount.Currency,
            InterestRate = product.InterestRate.Percentage,
            MinimumTermMonths = product.MinimumTermMonths,
            MaximumTermMonths = product.MaximumTermMonths,
            LenderId = product.LenderId,
            Status = product.Status.ToString(),
            CreatedAtUtc = product.CreatedAtUtc
        };
    }
}
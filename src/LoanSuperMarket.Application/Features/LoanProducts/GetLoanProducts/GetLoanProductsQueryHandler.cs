using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;

public sealed class GetLoanProductsQueryHandler
    : IRequestHandler<GetLoanProductsQuery, IReadOnlyList<LoanProductDto>>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public GetLoanProductsQueryHandler(ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<IReadOnlyList<LoanProductDto>> Handle(
        GetLoanProductsQuery request,
        CancellationToken cancellationToken)
    {
        var loanProducts = await _loanProductRepository.GetAllAsync(cancellationToken);

        return loanProducts
            .Select(x => new LoanProductDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                MinimumAmount = x.MinimumAmount.Amount,
                MaximumAmount = x.MaximumAmount.Amount,
                Currency = x.MinimumAmount.Currency,
                InterestRate = x.InterestRate.Percentage,
                MinimumTermMonths = x.MinimumTermMonths,
                MaximumTermMonths = x.MaximumTermMonths,
                LenderId = x.LenderId,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}
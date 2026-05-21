using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductsPaged;

public sealed class GetLoanProductsPagedQueryHandler
    : IRequestHandler<GetLoanProductsPagedQuery, PagedResult<LoanProductDto>>
{
    private readonly ILoanProductRepository _loanProductRepository;

    public GetLoanProductsPagedQueryHandler(
        ILoanProductRepository loanProductRepository)
    {
        _loanProductRepository = loanProductRepository;
    }

    public async Task<PagedResult<LoanProductDto>> Handle(
        GetLoanProductsPagedQuery request,
        CancellationToken cancellationToken)
    {
        return await _loanProductRepository.GetPagedAsync(
            request.Request,
            cancellationToken);
    }
}
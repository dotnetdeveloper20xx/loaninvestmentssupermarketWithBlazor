using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductsPaged;

public sealed record GetLoanProductsPagedQuery(
    GridQueryRequest Request) : IRequest<PagedResult<LoanProductDto>>;
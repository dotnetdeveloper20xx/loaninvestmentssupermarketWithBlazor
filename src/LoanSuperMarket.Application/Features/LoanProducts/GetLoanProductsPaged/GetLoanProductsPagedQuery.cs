using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductsPaged;

public sealed record GetLoanProductsPagedQuery(
    GridQueryRequest Request) : IRequest<PagedResult<LoanProductDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}
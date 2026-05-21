using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;

public sealed record GetLoanProductsQuery : IRequest<IReadOnlyList<LoanProductDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}
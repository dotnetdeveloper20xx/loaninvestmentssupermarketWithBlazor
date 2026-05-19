using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;

public sealed record GetLoanProductsQuery : IRequest<IReadOnlyList<LoanProductDto>>;
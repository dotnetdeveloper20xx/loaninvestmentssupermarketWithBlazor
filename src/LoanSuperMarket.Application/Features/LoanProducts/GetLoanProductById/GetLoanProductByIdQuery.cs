using LoanSuperMarket.Shared.LoanProducts;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductById;

public sealed record GetLoanProductByIdQuery(Guid Id) : IRequest<LoanProductDto?>;
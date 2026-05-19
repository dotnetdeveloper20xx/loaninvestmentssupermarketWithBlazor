using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.ApproveLoanProduct;

public sealed record ApproveLoanProductCommand(Guid Id) : IRequest;
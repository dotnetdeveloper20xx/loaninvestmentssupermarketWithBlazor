using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.PublishLoanProduct;

public sealed record PublishLoanProductCommand(Guid Id) : IRequest;
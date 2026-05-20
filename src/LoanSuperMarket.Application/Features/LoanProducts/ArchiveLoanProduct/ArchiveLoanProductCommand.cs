using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.ArchiveLoanProduct;

public sealed record ArchiveLoanProductCommand(Guid Id) : IRequest;
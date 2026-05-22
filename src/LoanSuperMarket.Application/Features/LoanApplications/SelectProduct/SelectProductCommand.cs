using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.SelectProduct;

public sealed record SelectProductCommand(
    Guid ApplicationId,
    Guid LoanProductId) : IRequest;

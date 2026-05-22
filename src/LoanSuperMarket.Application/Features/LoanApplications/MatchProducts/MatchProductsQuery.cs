using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.MatchProducts;

public sealed record MatchProductsQuery(Guid ApplicationId)
    : IRequest<IReadOnlyList<MatchedProductDto>>;

using LoanSuperMarket.Shared.Lenders;
using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.GetLenders;

public sealed record GetLendersQuery : IRequest<IReadOnlyList<LenderDto>>;
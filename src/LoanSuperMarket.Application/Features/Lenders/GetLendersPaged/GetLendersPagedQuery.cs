using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.Lenders;
using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.GetLendersPaged;

public sealed record GetLendersPagedQuery(
    GridQueryRequest Request) : IRequest<PagedResult<LenderDto>>;
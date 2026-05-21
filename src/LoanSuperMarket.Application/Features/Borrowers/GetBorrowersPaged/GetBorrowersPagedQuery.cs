using LoanSuperMarket.Shared.Borrowers;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowersPaged;

public sealed record GetBorrowersPagedQuery(
    GridQueryRequest Request) : IRequest<PagedResult<BorrowerDto>>;
using LoanSuperMarket.Shared.Borrowers;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowers;

public sealed record GetBorrowersQuery : IRequest<IReadOnlyList<BorrowerDto>>;
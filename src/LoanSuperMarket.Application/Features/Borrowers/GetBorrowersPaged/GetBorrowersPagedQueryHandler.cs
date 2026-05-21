using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Borrowers;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowersPaged;

public sealed class GetBorrowersPagedQueryHandler
    : IRequestHandler<GetBorrowersPagedQuery, PagedResult<BorrowerDto>>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public GetBorrowersPagedQueryHandler(
        IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<PagedResult<BorrowerDto>> Handle(
        GetBorrowersPagedQuery request,
        CancellationToken cancellationToken)
    {
        return await _borrowerRepository.GetPagedAsync(
            request.Request,
            cancellationToken);
    }
}
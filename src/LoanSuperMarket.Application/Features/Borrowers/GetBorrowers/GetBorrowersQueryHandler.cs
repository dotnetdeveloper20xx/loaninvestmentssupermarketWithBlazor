using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Borrowers;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.GetBorrowers;

public sealed class GetBorrowersQueryHandler
    : IRequestHandler<GetBorrowersQuery, IReadOnlyList<BorrowerDto>>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public GetBorrowersQueryHandler(IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<IReadOnlyList<BorrowerDto>> Handle(
        GetBorrowersQuery request,
        CancellationToken cancellationToken)
    {
        var borrowers = await _borrowerRepository.GetAllAsync(cancellationToken);

        return borrowers
            .Select(x => new BorrowerDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                FullName = x.FullName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                DateOfBirth = x.DateOfBirth,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}
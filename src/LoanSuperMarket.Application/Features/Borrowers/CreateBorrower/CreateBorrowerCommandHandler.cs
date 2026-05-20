using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.CreateBorrower;

public sealed class CreateBorrowerCommandHandler
    : IRequestHandler<CreateBorrowerCommand, Guid>
{
    private readonly IBorrowerRepository _borrowerRepository;

    public CreateBorrowerCommandHandler(IBorrowerRepository borrowerRepository)
    {
        _borrowerRepository = borrowerRepository;
    }

    public async Task<Guid> Handle(
        CreateBorrowerCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await _borrowerRepository.EmailExistsAsync(
            request.Email,
            cancellationToken);

        if (emailExists)
        {
            throw new DomainException("A borrower with this email address already exists.");
        }

        var borrower = Borrower.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth);

        await _borrowerRepository.AddAsync(borrower, cancellationToken);
        await _borrowerRepository.SaveChangesAsync(cancellationToken);

        return borrower.Id;
    }
}
using MediatR;

namespace LoanSuperMarket.Application.Features.Borrowers.CreateBorrower;

public sealed record CreateBorrowerCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime DateOfBirth) : IRequest<Guid>;
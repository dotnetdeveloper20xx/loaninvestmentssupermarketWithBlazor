using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.CreateLender;

public sealed record CreateLenderCommand(
    string CompanyName,
    string ContactName,
    string Email,
    string PhoneNumber,
    decimal AvailableFunds) : IRequest<Guid>;
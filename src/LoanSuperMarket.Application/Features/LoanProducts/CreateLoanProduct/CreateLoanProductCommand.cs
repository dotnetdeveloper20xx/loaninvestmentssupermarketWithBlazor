using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed record CreateLoanProductCommand(
    string Title,
    string Description,
    decimal MinimumAmount,
    decimal MaximumAmount,
    decimal InterestRate,
    int MinimumTermMonths,
    int MaximumTermMonths,
    Guid LenderId) : IRequest<Guid>;
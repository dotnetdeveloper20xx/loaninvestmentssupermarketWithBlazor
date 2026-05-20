using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateLoanApplication;

public sealed record CreateLoanApplicationCommand(
    Guid BorrowerId,
    Guid LoanProductId,
    decimal RequestedAmount,
    int TermMonths,
    string Purpose) : IRequest<Guid>;
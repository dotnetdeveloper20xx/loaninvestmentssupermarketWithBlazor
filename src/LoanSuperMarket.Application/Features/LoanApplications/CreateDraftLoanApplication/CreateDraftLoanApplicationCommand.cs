using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateDraftLoanApplication;

public sealed record CreateDraftLoanApplicationCommand(
    decimal RequestedAmount,
    int TermMonths,
    string Purpose) : IRequest<Guid>;

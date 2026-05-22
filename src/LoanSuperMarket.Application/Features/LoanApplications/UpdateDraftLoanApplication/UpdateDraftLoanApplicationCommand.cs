using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.UpdateDraftLoanApplication;

public sealed record UpdateDraftLoanApplicationCommand(
    Guid ApplicationId,
    decimal RequestedAmount,
    int TermMonths,
    string Purpose) : IRequest;

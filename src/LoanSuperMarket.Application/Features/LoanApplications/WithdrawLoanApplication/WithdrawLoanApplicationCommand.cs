using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.WithdrawLoanApplication;

public sealed record WithdrawLoanApplicationCommand(Guid ApplicationId) : IRequest;

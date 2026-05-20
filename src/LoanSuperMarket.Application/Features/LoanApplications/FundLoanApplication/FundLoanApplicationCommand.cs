using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.FundLoanApplication;

public sealed record FundLoanApplicationCommand(Guid Id) : IRequest;
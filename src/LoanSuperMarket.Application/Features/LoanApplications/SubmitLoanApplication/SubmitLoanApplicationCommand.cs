using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.SubmitLoanApplication;

public sealed record SubmitLoanApplicationCommand(Guid ApplicationId) : IRequest;

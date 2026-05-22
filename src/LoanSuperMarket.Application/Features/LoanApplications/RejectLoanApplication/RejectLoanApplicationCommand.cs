using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;

public sealed record RejectLoanApplicationCommand(Guid Id, string Reason) : IRequest;

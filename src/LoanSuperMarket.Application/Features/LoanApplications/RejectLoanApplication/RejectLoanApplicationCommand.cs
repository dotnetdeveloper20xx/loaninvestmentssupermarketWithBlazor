using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;

public sealed record RejectLoanApplicationCommand(Guid Id) : IRequest;
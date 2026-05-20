using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.ApproveLoanApplication;

public sealed record ApproveLoanApplicationCommand(Guid Id) : IRequest;
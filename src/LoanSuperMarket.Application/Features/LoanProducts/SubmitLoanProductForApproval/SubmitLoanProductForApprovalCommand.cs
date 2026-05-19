using MediatR;

namespace LoanSuperMarket.Application.Features.LoanProducts.SubmitLoanProductForApproval;

public sealed record SubmitLoanProductForApprovalCommand(Guid Id) : IRequest;
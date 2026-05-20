using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.MarkLoanApplicationUnderReview;

public sealed record MarkLoanApplicationUnderReviewCommand(Guid Id) : IRequest;
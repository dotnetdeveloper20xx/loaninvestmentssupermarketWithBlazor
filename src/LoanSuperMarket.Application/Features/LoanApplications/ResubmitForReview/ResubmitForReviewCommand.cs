using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.ResubmitForReview;

public sealed record ResubmitForReviewCommand(Guid ApplicationId) : IRequest;

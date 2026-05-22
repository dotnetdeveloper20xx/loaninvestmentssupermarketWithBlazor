using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectDocument;

public sealed record RejectDocumentCommand(
    Guid ApplicationId,
    Guid DocumentId,
    string RejectionNote) : IRequest;

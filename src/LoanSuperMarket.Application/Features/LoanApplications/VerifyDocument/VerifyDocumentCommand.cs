using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.VerifyDocument;

public sealed record VerifyDocumentCommand(
    Guid ApplicationId,
    Guid DocumentId) : IRequest;

using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RemoveDocument;

public sealed record RemoveDocumentCommand(
    Guid ApplicationId,
    Guid DocumentId) : IRequest;

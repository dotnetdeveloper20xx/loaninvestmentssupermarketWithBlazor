using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RequestAdditionalDocuments;

public sealed record RequestAdditionalDocumentsCommand(
    Guid ApplicationId,
    string Note) : IRequest;

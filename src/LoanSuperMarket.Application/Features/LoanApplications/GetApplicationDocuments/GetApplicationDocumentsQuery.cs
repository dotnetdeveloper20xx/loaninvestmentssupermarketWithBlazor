using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDocuments;

public sealed record GetApplicationDocumentsQuery(Guid ApplicationId)
    : IRequest<IReadOnlyList<ApplicationDocumentDto>>;

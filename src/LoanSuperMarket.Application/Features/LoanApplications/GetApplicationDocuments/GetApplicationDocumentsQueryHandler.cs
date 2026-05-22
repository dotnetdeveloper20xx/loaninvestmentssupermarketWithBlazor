using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDocuments;

public sealed class GetApplicationDocumentsQueryHandler
    : IRequestHandler<GetApplicationDocumentsQuery, IReadOnlyList<ApplicationDocumentDto>>
{
    private readonly IApplicationDocumentRepository _documentRepository;

    public GetApplicationDocumentsQueryHandler(IApplicationDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<ApplicationDocumentDto>> Handle(
        GetApplicationDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetByApplicationIdAsync(
            request.ApplicationId, cancellationToken);

        return documents.Select(d => new ApplicationDocumentDto(
            d.Id,
            d.FileName,
            (int)d.Type,
            (int)d.Status,
            d.UploadedAtUtc,
            d.VerifiedBy,
            d.VerifiedAtUtc,
            d.RejectionNote)).ToList();
    }
}

using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;

public sealed class UploadDocumentCommandHandler
    : IRequestHandler<UploadDocumentCommand, Guid>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IApplicationDocumentRepository _documentRepository;
    private readonly IDocumentStorageService _storageService;

    public UploadDocumentCommandHandler(
        ILoanApplicationRepository applicationRepository,
        IApplicationDocumentRepository documentRepository,
        IDocumentStorageService storageService)
    {
        _applicationRepository = applicationRepository;
        _documentRepository = documentRepository;
        _storageService = storageService;
    }

    public async Task<Guid> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        if (application.Status != LoanApplicationStatus.Draft &&
            application.Status != LoanApplicationStatus.DocumentsRequested)
        {
            throw new DomainException(
                "Documents can only be uploaded when the application is in Draft or DocumentsRequested status.");
        }

        var storageReference = await _storageService.StoreAsync(
            request.FileStream, request.FileName, cancellationToken);

        var document = ApplicationDocument.Create(
            request.ApplicationId,
            request.DocumentType,
            request.FileName,
            storageReference);

        await _documentRepository.AddAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}

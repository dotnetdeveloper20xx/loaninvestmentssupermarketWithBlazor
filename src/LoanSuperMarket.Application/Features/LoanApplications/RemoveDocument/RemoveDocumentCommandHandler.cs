using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RemoveDocument;

public sealed class RemoveDocumentCommandHandler
    : IRequestHandler<RemoveDocumentCommand>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IApplicationDocumentRepository _documentRepository;
    private readonly IDocumentStorageService _storageService;

    public RemoveDocumentCommandHandler(
        ILoanApplicationRepository applicationRepository,
        IApplicationDocumentRepository documentRepository,
        IDocumentStorageService storageService)
    {
        _applicationRepository = applicationRepository;
        _documentRepository = documentRepository;
        _storageService = storageService;
    }

    public async Task Handle(
        RemoveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        if (application.Status != LoanApplicationStatus.Draft &&
            application.Status != LoanApplicationStatus.DocumentsRequested)
        {
            throw new DomainException(
                "Documents can only be removed when the application is in Draft or DocumentsRequested status.");
        }

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new DomainException("Document was not found.");

        if (document.LoanApplicationId != request.ApplicationId)
        {
            throw new DomainException("Document does not belong to the specified application.");
        }

        await _storageService.DeleteAsync(document.StorageReference, cancellationToken);
        await _documentRepository.RemoveAsync(document, cancellationToken);
        await _documentRepository.SaveChangesAsync(cancellationToken);
    }
}

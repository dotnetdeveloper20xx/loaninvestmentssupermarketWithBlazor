using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.VerifyDocument;

public sealed class VerifyDocumentCommandHandler
    : IRequestHandler<VerifyDocumentCommand>
{
    private readonly IApplicationDocumentRepository _documentRepository;
    private readonly ICurrentUserService _currentUserService;

    public VerifyDocumentCommandHandler(
        IApplicationDocumentRepository documentRepository,
        ICurrentUserService currentUserService)
    {
        _documentRepository = documentRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        VerifyDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new DomainException("Document was not found.");

        if (document.LoanApplicationId != request.ApplicationId)
        {
            throw new DomainException("Document does not belong to the specified application.");
        }

        var userId = _currentUserService.UserId
            ?? throw new DomainException("User is not authenticated.");

        document.Verify(userId);

        await _documentRepository.SaveChangesAsync(cancellationToken);
    }
}

using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectDocument;

public sealed class RejectDocumentCommandHandler
    : IRequestHandler<RejectDocumentCommand>
{
    private readonly IApplicationDocumentRepository _documentRepository;
    private readonly ICurrentUserService _currentUserService;

    public RejectDocumentCommandHandler(
        IApplicationDocumentRepository documentRepository,
        ICurrentUserService currentUserService)
    {
        _documentRepository = documentRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        RejectDocumentCommand request,
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

        document.Reject(userId, request.RejectionNote);

        await _documentRepository.SaveChangesAsync(cancellationToken);
    }
}

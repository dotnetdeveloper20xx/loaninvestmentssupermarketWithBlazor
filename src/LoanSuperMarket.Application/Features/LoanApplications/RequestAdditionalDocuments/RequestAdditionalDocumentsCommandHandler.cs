using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RequestAdditionalDocuments;

public sealed class RequestAdditionalDocumentsCommandHandler
    : IRequestHandler<RequestAdditionalDocumentsCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public RequestAdditionalDocumentsCommandHandler(
        ILoanApplicationRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        RequestAdditionalDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        var userId = _currentUserService.UserId
            ?? throw new DomainException("User is not authenticated.");

        application.RequestDocuments(request.Note, userId);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

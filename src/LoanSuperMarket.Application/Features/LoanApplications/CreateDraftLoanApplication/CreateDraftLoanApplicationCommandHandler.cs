using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateDraftLoanApplication;

public sealed class CreateDraftLoanApplicationCommandHandler
    : IRequestHandler<CreateDraftLoanApplicationCommand, Guid>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDraftLoanApplicationCommandHandler(
        ILoanApplicationRepository applicationRepository,
        IBorrowerRepository borrowerRepository,
        ICurrentUserService currentUserService)
    {
        _applicationRepository = applicationRepository;
        _borrowerRepository = borrowerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateDraftLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User is not authenticated.");

        var borrower = await _borrowerRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Borrower profile was not found.");

        var application = LoanApplication.CreateDraft(
            borrower.Id,
            request.RequestedAmount,
            request.TermMonths,
            request.Purpose);

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _applicationRepository.SaveChangesAsync(cancellationToken);

        return application.Id;
    }
}

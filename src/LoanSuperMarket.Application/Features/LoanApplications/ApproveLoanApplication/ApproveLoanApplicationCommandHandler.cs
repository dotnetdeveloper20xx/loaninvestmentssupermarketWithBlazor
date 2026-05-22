using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.ApproveLoanApplication;

public sealed class ApproveLoanApplicationCommandHandler
    : IRequestHandler<ApproveLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public ApproveLoanApplicationCommandHandler(
        ILoanApplicationRepository repository,
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        ApproveLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        var userId = _currentUserService.UserId
            ?? throw new DomainException("User is not authenticated.");

        application.Approve(request.Reason, userId);

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "Approved",
                $"Loan application was approved. Reason: {request.Reason}"),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

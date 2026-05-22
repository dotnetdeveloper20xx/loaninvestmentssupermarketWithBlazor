using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;

public sealed class RejectLoanApplicationCommandHandler
    : IRequestHandler<RejectLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public RejectLoanApplicationCommandHandler(
        ILoanApplicationRepository repository,
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        RejectLoanApplicationCommand request,
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

        application.Reject(request.Reason, userId);

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "Rejected",
                $"Loan application was rejected. Reason: {request.Reason}"),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

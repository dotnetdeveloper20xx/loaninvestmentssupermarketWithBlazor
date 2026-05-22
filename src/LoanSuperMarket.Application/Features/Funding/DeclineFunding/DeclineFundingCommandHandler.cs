using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.DeclineFunding;

public sealed class DeclineFundingCommandHandler
    : IRequestHandler<DeclineFundingCommand, ApiResponse<string>>
{
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeclineFundingCommandHandler(
        ILoanApplicationRepository loanApplicationRepository,
        IAuditLogRepository auditLogRepository)
    {
        _loanApplicationRepository = loanApplicationRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        DeclineFundingCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _loanApplicationRepository.GetByIdAsync(
            request.ApplicationId, cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application not found.");
        }

        if (application.Status != LoanApplicationStatus.Approved)
        {
            throw new DomainException(
                "Only approved applications can be declined for funding.");
        }

        // Record decline via audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "FundingDeclined",
                $"Funding declined by lender {request.LenderId}. Reason: {request.DeclineReason}"),
            cancellationToken);

        await _loanApplicationRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            "Funding declined.",
            "The application funding has been declined.");
    }
}

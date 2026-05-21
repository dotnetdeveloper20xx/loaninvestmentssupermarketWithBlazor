using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.MarkLoanApplicationUnderReview;

public sealed class MarkLoanApplicationUnderReviewCommandHandler
    : IRequestHandler<MarkLoanApplicationUnderReviewCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public MarkLoanApplicationUnderReviewCommandHandler(
        ILoanApplicationRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(
        MarkLoanApplicationUnderReviewCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.MarkUnderReview();

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "UnderReview",
                "Loan application was moved into underwriting review."),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
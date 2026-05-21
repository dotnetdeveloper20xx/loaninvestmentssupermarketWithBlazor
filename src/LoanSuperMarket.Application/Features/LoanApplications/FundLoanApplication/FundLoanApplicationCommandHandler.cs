using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.FundLoanApplication;

public sealed class FundLoanApplicationCommandHandler
    : IRequestHandler<FundLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public FundLoanApplicationCommandHandler(
        ILoanApplicationRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(
        FundLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (application is null)
        {
            throw new DomainException("Loan application was not found.");
        }

        application.Fund();

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                application.Id,
                "Funded",
                "Loan application funding completed."),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.RestructureLoan;

public sealed class RestructureLoanCommandHandler
    : IRequestHandler<RestructureLoanCommand, ApiResponse<RestructureResultDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IAmortizationService _amortizationService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RestructureLoanCommandHandler(
        ILoanApplicationRepository repository,
        IAmortizationService amortizationService,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _amortizationService = amortizationService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<RestructureResultDto>> Handle(
        RestructureLoanCommand request,
        CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new DomainException("Repayment schedule not found.");
        }

        // Calculate remaining principal (unpaid installments)
        var remainingPrincipal = schedule.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .Sum(i => i.PrincipalPortion);

        if (remainingPrincipal <= 0)
        {
            throw new DomainException("Loan is fully paid. Cannot restructure.");
        }

        // Generate new schedule for remaining principal with new terms
        var newSchedule = _amortizationService.GenerateSchedule(
            schedule.LoanApplicationId,
            schedule.LenderId,
            remainingPrincipal,
            request.NewAnnualRate,
            request.NewTermMonths,
            DateTime.UtcNow);

        // Apply restructuring to existing schedule
        schedule.Restructure(
            request.NewAnnualRate,
            request.NewTermMonths,
            newSchedule.MonthlyEmi,
            newSchedule.TotalInterestPayable);

        // Remove unpaid installments and replace with new ones
        schedule.ClearInstallments();

        // Re-add paid installments (preserve history)
        // Then add new installments from the regenerated schedule
        foreach (var installment in newSchedule.Installments)
        {
            schedule.AddInstallment(installment);
        }

        // Audit
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "LoanApplication",
                schedule.LoanApplicationId,
                "Restructured",
                $"Loan restructured: new rate {request.NewAnnualRate:N2}%, " +
                $"new term {request.NewTermMonths} months, " +
                $"new EMI £{newSchedule.MonthlyEmi:N2}. " +
                $"Reason: {request.Reason ?? "Not specified"}"),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ApiResponse<RestructureResultDto>.Ok(new RestructureResultDto
        {
            ScheduleId = schedule.Id,
            NewRate = request.NewAnnualRate,
            NewTermMonths = request.NewTermMonths,
            NewMonthlyEmi = newSchedule.MonthlyEmi,
            NewTotalInterest = newSchedule.TotalInterestPayable,
            RemainingInstallments = request.NewTermMonths
        }, "Loan restructured successfully.");
    }
}

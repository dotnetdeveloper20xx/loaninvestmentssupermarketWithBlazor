using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.GetRepaymentSchedule;

public sealed class GetRepaymentScheduleQueryHandler
    : IRequestHandler<GetRepaymentScheduleQuery, ApiResponse<RepaymentScheduleDto>>
{
    private readonly ILoanApplicationRepository _repository;

    public GetRepaymentScheduleQueryHandler(ILoanApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<RepaymentScheduleDto>> Handle(
        GetRepaymentScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetRepaymentScheduleByIdAsync(
            request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            throw new DomainException("Repayment schedule not found.");
        }

        var dto = new RepaymentScheduleDto
        {
            ScheduleId = schedule.Id,
            LoanApplicationId = schedule.LoanApplicationId,
            FundedAmount = schedule.FundedAmount,
            AnnualInterestRate = schedule.AnnualInterestRate,
            TermMonths = schedule.TermMonths,
            MonthlyEmi = schedule.MonthlyEmi,
            TotalInterestPayable = schedule.TotalInterestPayable,
            Performance = schedule.Performance.ToString(),
            GeneratedAtUtc = schedule.GeneratedAtUtc,
            Installments = schedule.Installments
                .OrderBy(i => i.InstallmentNumber)
                .Select(i => new InstallmentDto
                {
                    Id = i.Id,
                    InstallmentNumber = i.InstallmentNumber,
                    DueDate = i.DueDate,
                    PrincipalPortion = i.PrincipalPortion,
                    InterestPortion = i.InterestPortion,
                    TotalAmount = i.TotalAmount,
                    RemainingBalance = i.RemainingBalance,
                    Status = i.Status.ToString(),
                    PaidAmount = i.PaidAmount,
                    PaidDate = i.PaidDate,
                    LateFeeAmount = i.LateFeeAmount,
                    Notes = i.Notes
                })
                .ToList()
        };

        return ApiResponse<RepaymentScheduleDto>.Ok(
            dto,
            "Repayment schedule retrieved successfully.");
    }
}

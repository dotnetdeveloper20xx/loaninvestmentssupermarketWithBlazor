using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Application.Common.Specifications;

/// <summary>
/// Specification for querying active (non-defaulted) repayment schedules.
/// </summary>
public sealed class ActiveSchedulesSpecification : Specification<RepaymentSchedule>
{
    public ActiveSchedulesSpecification()
    {
        Criteria = s => s.Performance != LoanPerformance.Defaulted;
        AddInclude(s => s.Installments);
    }
}

/// <summary>
/// Specification for querying schedules by lender with installments.
/// </summary>
public sealed class SchedulesByLenderSpecification : Specification<RepaymentSchedule>
{
    public SchedulesByLenderSpecification(Guid lenderId)
    {
        Criteria = s => s.LenderId == lenderId;
        AddInclude(s => s.Installments);
        AddInclude(s => s.LoanApplication!);
        OrderByDescending = s => s.GeneratedAtUtc;
    }
}

/// <summary>
/// Specification for querying defaulted schedules for collections.
/// </summary>
public sealed class DefaultedSchedulesSpecification : Specification<RepaymentSchedule>
{
    public DefaultedSchedulesSpecification()
    {
        Criteria = s => s.Performance == LoanPerformance.Defaulted;
        AddInclude(s => s.Installments);
        AddInclude(s => s.LoanApplication!);
    }
}

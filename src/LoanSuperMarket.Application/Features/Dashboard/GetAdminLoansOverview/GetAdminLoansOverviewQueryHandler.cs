using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetAdminLoansOverview;

public sealed class GetAdminLoansOverviewQueryHandler
    : IRequestHandler<GetAdminLoansOverviewQuery, ApiResponse<AdminLoansOverviewDto>>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILenderRepository _lenderRepository;
    private readonly IBorrowerRepository _borrowerRepository;

    public GetAdminLoansOverviewQueryHandler(
        ILoanApplicationRepository repository,
        ILenderRepository lenderRepository,
        IBorrowerRepository borrowerRepository)
    {
        _repository = repository;
        _lenderRepository = lenderRepository;
        _borrowerRepository = borrowerRepository;
    }

    public async Task<ApiResponse<AdminLoansOverviewDto>> Handle(
        GetAdminLoansOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var allSchedules = await _repository.GetAllActiveSchedulesAsync(cancellationToken);

        // Also get defaulted ones for the full picture
        var lenders = await _lenderRepository.GetAllAsync(cancellationToken);

        // Get ALL schedules by iterating lenders
        var allLoanSchedules = new List<Domain.Entities.RepaymentSchedule>();
        foreach (var lender in lenders)
        {
            var schedules = await _repository.GetSchedulesByLenderIdAsync(
                lender.Id, cancellationToken);
            allLoanSchedules.AddRange(schedules);
        }

        // Apply filters
        var filtered = allLoanSchedules.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.PerformanceFilter)
            && Enum.TryParse<LoanPerformance>(request.PerformanceFilter, out var perfFilter))
        {
            filtered = filtered.Where(s => s.Performance == perfFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.LenderFilter))
        {
            var matchingLender = lenders.FirstOrDefault(l =>
                l.CompanyName.Contains(request.LenderFilter, StringComparison.OrdinalIgnoreCase));
            if (matchingLender is not null)
            {
                filtered = filtered.Where(s => s.LenderId == matchingLender.Id);
            }
        }

        var loans = new List<AdminLoanItemDto>();

        foreach (var schedule in filtered)
        {
            var lender = lenders.FirstOrDefault(l => l.Id == schedule.LenderId);
            var borrowerName = "Unknown";

            if (schedule.LoanApplication is not null)
            {
                var borrower = await _borrowerRepository.GetByIdAsync(
                    schedule.LoanApplication.BorrowerId, cancellationToken);
                if (borrower is not null)
                    borrowerName = $"{borrower.FirstName} {borrower.LastName}";
            }

            loans.Add(new AdminLoanItemDto
            {
                ScheduleId = schedule.Id,
                LenderName = lender?.CompanyName ?? "Unknown",
                BorrowerName = borrowerName,
                FundedAmount = schedule.FundedAmount,
                EffectiveRate = schedule.AnnualInterestRate,
                TermMonths = schedule.TermMonths,
                Performance = schedule.Performance.ToString(),
                FundedDate = schedule.GeneratedAtUtc,
                PaidInstallments = schedule.Installments.Count(i => i.Status == InstallmentStatus.Paid),
                TotalInstallments = schedule.Installments.Count
            });
        }

        var totalAll = allLoanSchedules.Count;
        var defaultedCount = allLoanSchedules.Count(s => s.Performance == LoanPerformance.Defaulted);

        return ApiResponse<AdminLoansOverviewDto>.Ok(new AdminLoansOverviewDto
        {
            TotalActiveLoans = allLoanSchedules.Count(s => s.Performance != LoanPerformance.Defaulted),
            TotalDefaultedLoans = defaultedCount,
            TotalOutstandingPrincipal = allLoanSchedules.Sum(s =>
                s.FundedAmount - s.Installments
                    .Where(i => i.Status == InstallmentStatus.Paid)
                    .Sum(i => i.PrincipalPortion)),
            TotalFundedAllTime = allLoanSchedules.Sum(s => s.FundedAmount),
            PlatformDefaultRate = totalAll > 0
                ? decimal.Round((decimal)defaultedCount / totalAll * 100, 2)
                : 0,
            Loans = loans.OrderByDescending(l => l.FundedDate).ToList()
        }, "Admin loans overview retrieved successfully.");
    }
}

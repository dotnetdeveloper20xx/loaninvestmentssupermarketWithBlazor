using LoanSuperMarket.Shared.Dashboard;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// High-performance reporting service using stored procedures via Dapper.
/// </summary>
public interface IPlatformReportService
{
    Task<PlatformSummaryDto> GetPlatformSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyInterestReportDto>> GetMonthlyInterestReportAsync(
        Guid lenderId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

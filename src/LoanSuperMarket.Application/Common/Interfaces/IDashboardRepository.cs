using LoanSuperMarket.Shared.Dashboard;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken);
}
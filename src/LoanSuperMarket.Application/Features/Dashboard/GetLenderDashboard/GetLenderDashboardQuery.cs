using LoanSuperMarket.Application.Common.Behaviours;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;

public sealed class GetLenderDashboardQuery : IRequest<ApiResponse<LenderPortfolioDto>>, IResourceFilteredQuery, ICacheableQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }

    public string CacheKey => $"lender-portfolio-{FilterByUserId}";

    public int CacheMinutes => 2;
}

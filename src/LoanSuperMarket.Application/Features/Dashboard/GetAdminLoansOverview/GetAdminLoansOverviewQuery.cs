using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetAdminLoansOverview;

public sealed class GetAdminLoansOverviewQuery : IRequest<ApiResponse<AdminLoansOverviewDto>>
{
    public string? PerformanceFilter { get; set; }

    public string? LenderFilter { get; set; }
}

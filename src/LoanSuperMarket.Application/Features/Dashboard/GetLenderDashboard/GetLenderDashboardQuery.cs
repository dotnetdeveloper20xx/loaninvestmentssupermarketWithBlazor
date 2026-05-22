using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;

public sealed class GetLenderDashboardQuery : IRequest<ApiResponse<LenderPortfolioDto>>, IResourceFilteredQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}

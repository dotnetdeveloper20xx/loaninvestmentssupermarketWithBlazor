using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetInvestorAnalytics;

public sealed class GetInvestorAnalyticsQuery : IRequest<ApiResponse<InvestorAnalyticsDto>>, IResourceFilteredQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}

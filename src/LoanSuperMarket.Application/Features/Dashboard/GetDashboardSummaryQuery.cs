using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard;

public sealed record GetDashboardSummaryQuery
    : IRequest<DashboardSummaryDto>;
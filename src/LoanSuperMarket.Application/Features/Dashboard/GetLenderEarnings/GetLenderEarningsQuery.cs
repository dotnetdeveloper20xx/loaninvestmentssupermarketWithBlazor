using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderEarnings;

public sealed class GetLenderEarningsQuery : IRequest<ApiResponse<LenderEarningsDto>>, IResourceFilteredQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}

using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.GetRepaymentSchedule;

public sealed class GetRepaymentScheduleQuery : IRequest<ApiResponse<RepaymentScheduleDto>>, IResourceFilteredQuery
{
    public Guid ScheduleId { get; set; }

    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}

using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.RestructureLoan;

public sealed record RestructureLoanCommand(
    Guid ScheduleId,
    decimal NewAnnualRate,
    int NewTermMonths,
    string? Reason) : IRequest<ApiResponse<RestructureResultDto>>;

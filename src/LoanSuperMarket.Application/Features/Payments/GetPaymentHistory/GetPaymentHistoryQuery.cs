using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.GetPaymentHistory;

public sealed record GetPaymentHistoryQuery(Guid ScheduleId)
    : IRequest<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>;

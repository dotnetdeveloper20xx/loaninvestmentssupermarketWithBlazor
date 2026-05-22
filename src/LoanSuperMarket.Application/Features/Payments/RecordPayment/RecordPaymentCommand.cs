using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid ScheduleId,
    decimal Amount,
    DateTime PaymentDate) : IRequest<ApiResponse<PaymentResultDto>>;

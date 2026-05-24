using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;

namespace LoanSuperMarket.Application.Features.Payments.RecordBulkPayment;

public sealed record RecordBulkPaymentCommand(
    Guid ScheduleId,
    decimal Amount,
    DateTime PaymentDate) : IRequest<ApiResponse<BulkPaymentResultDto>>;

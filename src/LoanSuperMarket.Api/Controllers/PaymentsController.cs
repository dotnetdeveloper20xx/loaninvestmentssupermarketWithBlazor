using LoanSuperMarket.Application.Features.Payments.GetPaymentHistory;
using LoanSuperMarket.Application.Features.Payments.GetRepaymentSchedule;
using LoanSuperMarket.Application.Features.Payments.RecordPayment;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{scheduleId:guid}/pay")]
    public async Task<ActionResult<ApiResponse<PaymentResultDto>>> RecordPayment(
        Guid scheduleId,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordPaymentCommand(
            scheduleId,
            request.Amount,
            request.PaymentDate);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{scheduleId:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>> GetPaymentHistory(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetPaymentHistoryQuery(scheduleId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<ActionResult<ApiResponse<RepaymentScheduleDto>>> GetRepaymentSchedule(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var query = new GetRepaymentScheduleQuery { ScheduleId = scheduleId };
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}

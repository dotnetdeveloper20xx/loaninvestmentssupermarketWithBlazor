using LoanSuperMarket.Application.Features.Payments.GetPaymentHistory;
using LoanSuperMarket.Application.Features.Payments.GetRepaymentSchedule;
using LoanSuperMarket.Application.Features.Payments.RecordBulkPayment;
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

    [HttpPost("{scheduleId:guid}/pay-bulk")]
    public async Task<ActionResult<ApiResponse<BulkPaymentResultDto>>> RecordBulkPayment(
        Guid scheduleId,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordBulkPaymentCommand(
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

    [HttpGet("{scheduleId:guid}/export")]
    public async Task<IActionResult> ExportScheduleCsv(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        var query = new GetRepaymentScheduleQuery { ScheduleId = scheduleId };
        var result = await _sender.Send(query, cancellationToken);

        if (!result.Success || result.Data is null)
        {
            return NotFound();
        }

        var schedule = result.Data;
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Installment,Due Date,Principal,Interest,Total,Remaining Balance,Status,Paid Amount,Paid Date,Late Fee");

        foreach (var inst in schedule.Installments)
        {
            csv.AppendLine(string.Join(",",
                inst.InstallmentNumber,
                inst.DueDate.ToString("yyyy-MM-dd"),
                inst.PrincipalPortion.ToString("F2"),
                inst.InterestPortion.ToString("F2"),
                inst.TotalAmount.ToString("F2"),
                inst.RemainingBalance.ToString("F2"),
                inst.Status,
                inst.PaidAmount.ToString("F2"),
                inst.PaidDate?.ToString("yyyy-MM-dd") ?? "",
                inst.LateFeeAmount.ToString("F2")));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"repayment-schedule-{scheduleId:N}.csv");
    }
}

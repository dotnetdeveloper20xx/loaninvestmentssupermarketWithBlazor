using MediatR;

namespace LoanSuperMarket.Application.Common.Events;

/// <summary>
/// Domain event raised when a payment is recorded against a schedule.
/// </summary>
public sealed record PaymentRecordedEvent(
    Guid ScheduleId,
    int InstallmentNumber,
    decimal Amount,
    string Status) : INotification;

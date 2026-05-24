using MediatR;

namespace LoanSuperMarket.Application.Common.Events;

/// <summary>
/// Domain event raised when a loan enters default status.
/// </summary>
public sealed record LoanDefaultedEvent(
    Guid ScheduleId,
    Guid LenderId,
    Guid ApplicationId) : INotification;

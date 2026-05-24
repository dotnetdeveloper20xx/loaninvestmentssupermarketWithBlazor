using MediatR;

namespace LoanSuperMarket.Application.Common.Events;

/// <summary>
/// Domain event raised when a loan is successfully funded.
/// </summary>
public sealed record LoanFundedEvent(
    Guid ApplicationId,
    Guid LenderId,
    Guid ScheduleId,
    decimal Amount,
    string? BorrowerUserId) : INotification;

using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email confirmation link to the specified email address.
    /// </summary>
    Task SendEmailConfirmationAsync(
        string email,
        string userId,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a user's account status has been changed.
    /// </summary>
    Task SendAccountStatusChangedAsync(
        string email,
        string userName,
        AccountStatus previousStatus,
        AccountStatus newStatus,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to an applicant requesting additional documents.
    /// </summary>
    Task SendDocumentsRequestedAsync(
        string email,
        string userName,
        IReadOnlyList<string> requiredDocuments,
        CancellationToken cancellationToken = default);
}

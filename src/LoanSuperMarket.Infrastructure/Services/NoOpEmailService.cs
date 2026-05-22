using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Infrastructure.Services;

/// <summary>
/// A no-op implementation of IEmailService that does nothing.
/// Used as a placeholder until a real email provider is configured.
/// </summary>
public sealed class NoOpEmailService : IEmailService
{
    /// <inheritdoc />
    public Task SendEmailConfirmationAsync(
        string email,
        string userId,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAccountStatusChangedAsync(
        string email,
        string userName,
        AccountStatus previousStatus,
        AccountStatus newStatus,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendDocumentsRequestedAsync(
        string email,
        string userName,
        IReadOnlyList<string> requiredDocuments,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

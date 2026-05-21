using LoanSuperMarket.Application.Common.Exceptions;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace LoanSuperMarket.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that enforces credit and capital limits.
/// <list type="bullet">
///   <item>Borrowers: loan application amount must not exceed their CreditLimit.</item>
///   <item>Borrowers: active loan count must not exceed MaxActiveLoansPerBorrower.</item>
///   <item>Lenders: funding amount must not exceed their CapitalLimit.</item>
/// </list>
/// Only applies to authenticated users with the appropriate roles (Borrower or Lender).
/// </summary>
public sealed class LimitEnforcementBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly AccountSettings _accountSettings;

    public LimitEnforcementBehaviour(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IBorrowerRepository borrowerRepository,
        ILoanApplicationRepository loanApplicationRepository,
        IOptions<AccountSettings> accountSettings)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _borrowerRepository = borrowerRepository;
        _loanApplicationRepository = loanApplicationRepository;
        _accountSettings = accountSettings.Value;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only enforce limits on loan application or loan funding commands
        if (request is not ILoanApplicationCommand && request is not ILoanFundingCommand)
        {
            return await next(cancellationToken);
        }

        // Skip enforcement for unauthenticated requests
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await next(cancellationToken);
        }

        var user = await _identityService.GetUserByIdAsync(_currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            return await next(cancellationToken);
        }

        if (request is ILoanApplicationCommand loanCommand
            && _currentUserService.IsInRole("Borrower"))
        {
            await EnforceBorrowerLimits(loanCommand, user.CreditLimit, cancellationToken);
        }

        if (request is ILoanFundingCommand fundingCommand
            && _currentUserService.IsInRole("Lender"))
        {
            EnforceLenderCapitalLimit(fundingCommand, user.CapitalLimit);
        }

        return await next(cancellationToken);
    }

    /// <summary>
    /// Enforces borrower credit limit and maximum active loans restrictions.
    /// </summary>
    private async Task EnforceBorrowerLimits(
        ILoanApplicationCommand command,
        decimal? creditLimit,
        CancellationToken cancellationToken)
    {
        // Enforce credit limit
        if (creditLimit.HasValue && command.Amount > creditLimit.Value)
        {
            throw new LimitExceededException(
                "LIMIT_CREDIT_EXCEEDED",
                $"The requested loan amount of {command.Amount:C} exceeds your credit limit of {creditLimit.Value:C}.");
        }

        // Enforce maximum active loans per borrower
        var borrower = await _borrowerRepository.GetByUserIdAsync(
            _currentUserService.UserId!, cancellationToken);

        if (borrower is null)
        {
            // If no borrower profile is linked, skip the active loans check
            return;
        }

        var activeLoansCount = await _loanApplicationRepository.CountActiveByBorrowerIdAsync(
            borrower.Id, cancellationToken);

        if (activeLoansCount >= _accountSettings.MaxActiveLoansPerBorrower)
        {
            throw new LimitExceededException(
                "LIMIT_MAX_LOANS",
                $"You have reached the maximum number of active loans ({_accountSettings.MaxActiveLoansPerBorrower}). " +
                "Please wait for existing loans to complete before applying for a new one.");
        }
    }

    /// <summary>
    /// Enforces lender capital limit restrictions.
    /// </summary>
    private static void EnforceLenderCapitalLimit(ILoanFundingCommand command, decimal? capitalLimit)
    {
        if (capitalLimit.HasValue && command.Amount > capitalLimit.Value)
        {
            throw new LimitExceededException(
                "LIMIT_CAPITAL_EXCEEDED",
                $"The funding amount of {command.Amount:C} exceeds your capital limit of {capitalLimit.Value:C}.");
        }
    }
}

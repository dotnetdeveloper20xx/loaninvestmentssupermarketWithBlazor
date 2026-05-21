using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.ApproveRegistration;

/// <summary>
/// Command to approve a user's registration during the vetting workflow.
/// For Borrowers, CreditTier and CreditLimit are required.
/// For Lenders, CapitalLimit is required.
/// </summary>
public sealed record ApproveRegistrationCommand(
    string UserId,
    string Reason,
    CreditTier? CreditTier = null,
    decimal? CreditLimit = null,
    decimal? CapitalLimit = null) : IRequest<ApiResponse<string>>;

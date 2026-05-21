using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.RejectRegistration;

/// <summary>
/// Command to reject a user's registration during the vetting workflow.
/// </summary>
public sealed record RejectRegistrationCommand(
    string UserId,
    string Reason) : IRequest<ApiResponse<string>>;

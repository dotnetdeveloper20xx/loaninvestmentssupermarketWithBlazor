using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.RequestDocuments;

/// <summary>
/// Command to request additional documents from an applicant during the vetting workflow.
/// </summary>
public sealed record RequestDocumentsCommand(
    string UserId,
    IReadOnlyList<string> RequiredDocuments) : IRequest<ApiResponse<string>>;

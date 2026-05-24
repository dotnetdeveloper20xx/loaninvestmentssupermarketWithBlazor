using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetCollections;

public sealed class GetCollectionsQuery : IRequest<ApiResponse<IReadOnlyList<CollectionItemDto>>>;

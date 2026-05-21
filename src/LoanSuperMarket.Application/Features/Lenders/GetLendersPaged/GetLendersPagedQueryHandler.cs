using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Lenders;
using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.GetLendersPaged;

public sealed class GetLendersPagedQueryHandler
    : IRequestHandler<GetLendersPagedQuery, PagedResult<LenderDto>>
{
    private readonly ILenderRepository _lenderRepository;

    public GetLendersPagedQueryHandler(
        ILenderRepository lenderRepository)
    {
        _lenderRepository = lenderRepository;
    }

    public async Task<PagedResult<LenderDto>> Handle(
        GetLendersPagedQuery request,
        CancellationToken cancellationToken)
    {
        return await _lenderRepository.GetPagedAsync(
            request.Request,
            cancellationToken);
    }
}
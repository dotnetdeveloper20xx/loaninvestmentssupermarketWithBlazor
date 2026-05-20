using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Lenders;
using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.GetLenders;

public sealed class GetLendersQueryHandler
    : IRequestHandler<GetLendersQuery, IReadOnlyList<LenderDto>>
{
    private readonly ILenderRepository _lenderRepository;

    public GetLendersQueryHandler(ILenderRepository lenderRepository)
    {
        _lenderRepository = lenderRepository;
    }

    public async Task<IReadOnlyList<LenderDto>> Handle(
        GetLendersQuery request,
        CancellationToken cancellationToken)
    {
        var lenders = await _lenderRepository.GetAllAsync(cancellationToken);

        return lenders
            .Select(x => new LenderDto
            {
                Id = x.Id,
                CompanyName = x.CompanyName,
                ContactName = x.ContactName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                AvailableFunds = x.AvailableFunds,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();
    }
}
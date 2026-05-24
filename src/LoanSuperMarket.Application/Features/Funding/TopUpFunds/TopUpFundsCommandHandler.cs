using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.TopUpFunds;

public sealed class TopUpFundsCommandHandler
    : IRequestHandler<TopUpFundsCommand, ApiResponse<decimal>>
{
    private readonly ILenderRepository _lenderRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public TopUpFundsCommandHandler(
        ILenderRepository lenderRepository,
        IAuditLogRepository auditLogRepository)
    {
        _lenderRepository = lenderRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<decimal>> Handle(
        TopUpFundsCommand request,
        CancellationToken cancellationToken)
    {
        var lender = await _lenderRepository.GetByIdAsync(request.LenderId, cancellationToken);

        if (lender is null)
        {
            throw new DomainException("Lender not found.");
        }

        lender.TopUpFunds(request.Amount);

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "Lender",
                lender.Id,
                "FundsTopUp",
                $"Lender topped up £{request.Amount:N2}. New balance: £{lender.AvailableFunds:N2}."),
            cancellationToken);

        await _lenderRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<decimal>.Ok(
            lender.AvailableFunds,
            $"Funds topped up successfully. New balance: £{lender.AvailableFunds:N2}.");
    }
}

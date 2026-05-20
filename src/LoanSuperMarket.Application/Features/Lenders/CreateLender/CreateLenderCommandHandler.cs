using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using MediatR;

namespace LoanSuperMarket.Application.Features.Lenders.CreateLender;

public sealed class CreateLenderCommandHandler
    : IRequestHandler<CreateLenderCommand, Guid>
{
    private readonly ILenderRepository _lenderRepository;

    public CreateLenderCommandHandler(ILenderRepository lenderRepository)
    {
        _lenderRepository = lenderRepository;
    }

    public async Task<Guid> Handle(
        CreateLenderCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await _lenderRepository.EmailExistsAsync(
            request.Email,
            cancellationToken);

        if (emailExists)
        {
            throw new DomainException("A lender with this email address already exists.");
        }

        var lender = Lender.Create(
            request.CompanyName,
            request.ContactName,
            request.Email,
            request.PhoneNumber,
            request.AvailableFunds);

        await _lenderRepository.AddAsync(lender, cancellationToken);
        await _lenderRepository.SaveChangesAsync(cancellationToken);

        return lender.Id;
    }
}
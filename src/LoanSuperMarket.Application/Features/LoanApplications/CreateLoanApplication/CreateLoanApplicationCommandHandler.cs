using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.ValueObjects;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateLoanApplication;

public sealed class CreateLoanApplicationCommandHandler
    : IRequestHandler<CreateLoanApplicationCommand, Guid>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _loanProductRepository;

    public CreateLoanApplicationCommandHandler(
        ILoanApplicationRepository applicationRepository,
        IBorrowerRepository borrowerRepository,
        ILoanProductRepository loanProductRepository)
    {
        _applicationRepository = applicationRepository;
        _borrowerRepository = borrowerRepository;
        _loanProductRepository = loanProductRepository;
    }

    public async Task<Guid> Handle(
        CreateLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var borrower = await _borrowerRepository.GetByIdAsync(request.BorrowerId, cancellationToken);

        if (borrower is null)
        {
            throw new DomainException("Borrower was not found.");
        }

        var loanProduct = await _loanProductRepository.GetByIdAsync(request.LoanProductId, cancellationToken);

        if (loanProduct is null)
        {
            throw new DomainException("Loan product was not found.");
        }

        if (loanProduct.Status.ToString() != "Published")
        {
            throw new DomainException("Only published loan products can receive applications.");
        }

        if (request.RequestedAmount < loanProduct.MinimumAmount.Amount ||
            request.RequestedAmount > loanProduct.MaximumAmount.Amount)
        {
            throw new DomainException("Requested amount is outside the loan product amount range.");
        }

        if (request.TermMonths < loanProduct.MinimumTermMonths ||
            request.TermMonths > loanProduct.MaximumTermMonths)
        {
            throw new DomainException("Requested term is outside the loan product term range.");
        }

        var application = LoanApplication.Create(
            request.BorrowerId,
            request.LoanProductId,
            Money.Create(request.RequestedAmount),
            request.TermMonths,
            request.Purpose);

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _applicationRepository.SaveChangesAsync(cancellationToken);

        return application.Id;
    }
}
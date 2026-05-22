using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.SelectProduct;

public sealed class SelectProductCommandHandler
    : IRequestHandler<SelectProductCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly ILoanProductRepository _productRepository;

    public SelectProductCommandHandler(
        ILoanApplicationRepository repository,
        ILoanProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    public async Task Handle(
        SelectProductCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        var product = await _productRepository.GetByIdAsync(request.LoanProductId, cancellationToken)
            ?? throw new DomainException("Loan product was not found.");

        application.SelectProduct(request.LoanProductId);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}

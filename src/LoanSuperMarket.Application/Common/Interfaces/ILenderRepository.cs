using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface ILenderRepository
{
    Task AddAsync(Lender lender, CancellationToken cancellationToken);

    Task<Lender?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Lender>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
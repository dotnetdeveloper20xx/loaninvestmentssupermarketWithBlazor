using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IBorrowerRepository
{
    Task AddAsync(Borrower borrower, CancellationToken cancellationToken);

    Task<Borrower?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Borrower>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IApplicationDocumentRepository
{
    Task AddAsync(ApplicationDocument document, CancellationToken ct);

    Task<ApplicationDocument?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<ApplicationDocument>> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct);

    Task RemoveAsync(ApplicationDocument document, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}

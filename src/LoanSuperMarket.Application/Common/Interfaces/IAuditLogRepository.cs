using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Audit;

namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken);
}
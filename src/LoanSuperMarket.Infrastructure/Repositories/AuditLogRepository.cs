using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Audit;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        take = take is < 1 or > 100 ? 20 : take;

        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                Description = x.Description,
                PerformedBy = x.PerformedBy,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                Description = x.Description,
                PerformedBy = x.PerformedBy,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
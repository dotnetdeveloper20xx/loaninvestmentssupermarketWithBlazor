using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class ApplicationDocumentRepository : IApplicationDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationDocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ApplicationDocument document, CancellationToken ct)
    {
        await _context.ApplicationDocuments.AddAsync(document, ct);
    }

    public async Task<ApplicationDocument?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.ApplicationDocuments
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<ApplicationDocument>> GetByApplicationIdAsync(
        Guid applicationId, CancellationToken ct)
    {
        return await _context.ApplicationDocuments
            .Where(x => x.LoanApplicationId == applicationId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync(ct);
    }

    public Task RemoveAsync(ApplicationDocument document, CancellationToken ct)
    {
        _context.ApplicationDocuments.Remove(document);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}

using System.Data;
using Dapper;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Dashboard;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LoanSuperMarket.Infrastructure.Services;

/// <summary>
/// High-performance reporting using Dapper and stored procedures.
/// Bypasses EF Core for read-heavy aggregation queries.
/// </summary>
public sealed class DapperPlatformReportService : IPlatformReportService
{
    private readonly string _connectionString;

    public DapperPlatformReportService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");
    }

    public async Task<PlatformSummaryDto> GetPlatformSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        var result = await connection.QueryFirstOrDefaultAsync<PlatformSummaryDto>(
            "sp_GetPlatformSummary",
            commandType: CommandType.StoredProcedure);

        return result ?? new PlatformSummaryDto();
    }

    public async Task<IReadOnlyList<MonthlyInterestReportDto>> GetMonthlyInterestReportAsync(
        Guid lenderId, DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        var results = await connection.QueryAsync<MonthlyInterestReportDto>(
            "sp_GetMonthlyInterestReport",
            new { LenderId = lenderId, FromDate = fromDate, ToDate = toDate },
            commandType: CommandType.StoredProcedure);

        return results.ToList();
    }
}

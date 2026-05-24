using System.Net.Http.Json;
using LoanSuperMarket.Shared.Audit;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class DashboardApiClient
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<DashboardSummaryDto>?> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<DashboardSummaryDto>>(
            "api/dashboard/summary",
            cancellationToken);
    }

    public async Task<ApiResponse<LenderPortfolioDto>?> GetLenderPortfolioAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<LenderPortfolioDto>>(
            "api/dashboard/lender/portfolio",
            cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<LenderLoanDto>>?> GetLenderLoansAsync(
        string? performance = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(performance))
            queryParams.Add($"performance={Uri.EscapeDataString(performance)}");
        if (!string.IsNullOrWhiteSpace(sortBy))
            queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

        var url = "api/dashboard/lender/loans";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<LenderLoanDto>>>(
            url, cancellationToken);
    }

    public async Task<ApiResponse<LenderEarningsDto>?> GetLenderEarningsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<LenderEarningsDto>>(
            "api/dashboard/lender/earnings",
            cancellationToken);
    }

    public async Task<ApiResponse<InvestorAnalyticsDto>?> GetInvestorAnalyticsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<InvestorAnalyticsDto>>(
            "api/dashboard/lender/analytics",
            cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<BorrowerLoanDto>>?> GetBorrowerLoansAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<BorrowerLoanDto>>>(
            "api/dashboard/borrower/loans",
            cancellationToken);
    }

    public async Task<ApiResponse<BorrowerPaymentSummaryDto>?> GetBorrowerPaymentSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<BorrowerPaymentSummaryDto>>(
            "api/dashboard/borrower/upcoming",
            cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<AuditLogDto>>?> GetAuditTrailAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<AuditLogDto>>>(
            $"api/dashboard/audit/{entityName}/{entityId}",
            cancellationToken);
    }

    public async Task<ApiResponse<AdminLoansOverviewDto>?> GetAdminLoansOverviewAsync(
        string? performance = null,
        string? lender = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(performance))
            queryParams.Add($"performance={Uri.EscapeDataString(performance)}");
        if (!string.IsNullOrWhiteSpace(lender))
            queryParams.Add($"lender={Uri.EscapeDataString(lender)}");

        var url = "api/dashboard/admin/loans";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return await _httpClient.GetFromJsonAsync<ApiResponse<AdminLoansOverviewDto>>(
            url, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<CollectionItemDto>>?> GetCollectionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<CollectionItemDto>>>(
            "api/dashboard/admin/collections",
            cancellationToken);
    }
}
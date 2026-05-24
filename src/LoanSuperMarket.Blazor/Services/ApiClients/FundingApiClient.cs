using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class FundingApiClient
{
    private readonly HttpClient _httpClient;

    public FundingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<FundingQueueItemDto>>?> GetFundingQueueAsync(
        string? productTitle = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(productTitle))
            queryParams.Add($"productTitle={Uri.EscapeDataString(productTitle)}");
        if (minAmount.HasValue)
            queryParams.Add($"minAmount={minAmount.Value}");
        if (maxAmount.HasValue)
            queryParams.Add($"maxAmount={maxAmount.Value}");

        var url = "api/funding/queue";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<FundingQueueItemDto>>>(
            url, cancellationToken);
    }

    public async Task<ApiResponse<FundingApplicationDetailDto>?> GetApplicationDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<FundingApplicationDetailDto>>(
            $"api/funding/{applicationId}/details",
            cancellationToken);
    }

    public async Task<ApiResponse<FundingResultDto>?> AcceptFundingAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var request = new AcceptFundingRequest { ApplicationId = applicationId };
        var response = await _httpClient.PostAsJsonAsync(
            $"api/funding/{applicationId}/accept",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<FundingResultDto>>(
            cancellationToken);
    }

    public async Task<ApiResponse<string>?> DeclineFundingAsync(
        Guid applicationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var request = new DeclineFundingRequest
        {
            ApplicationId = applicationId,
            Reason = reason
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/funding/{applicationId}/decline",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }

    public async Task<ApiResponse<decimal>?> TopUpFundsAsync(
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var request = new TopUpFundsRequest { Amount = amount };
        var response = await _httpClient.PostAsJsonAsync(
            "api/funding/top-up",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<decimal>>(
            cancellationToken);
    }
}

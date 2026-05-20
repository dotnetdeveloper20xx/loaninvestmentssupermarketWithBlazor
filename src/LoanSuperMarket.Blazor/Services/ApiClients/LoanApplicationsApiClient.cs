using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class LoanApplicationsApiClient
{
    private readonly HttpClient _httpClient;

    public LoanApplicationsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<LoanApplicationDto>>?> GetLoanApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<LoanApplicationDto>>>(
            "api/loan-applications",
            cancellationToken);
    }

    public async Task<ApiResponse<Guid>?> CreateLoanApplicationAsync(
        CreateLoanApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/loan-applications",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            cancellationToken);
    }
}
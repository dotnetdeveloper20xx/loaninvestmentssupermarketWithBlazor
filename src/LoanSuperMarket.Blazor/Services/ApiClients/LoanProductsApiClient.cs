using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanProducts;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class LoanProductsApiClient
{
    private readonly HttpClient _httpClient;

    public LoanProductsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<IReadOnlyList<LoanProductDto>>?> GetLoanProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<LoanProductDto>>>(
            "api/loan-products",
            cancellationToken);
    }

    public async Task<ApiResponse<LoanProductDto>?> GetLoanProductByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<LoanProductDto>>(
            $"api/loan-products/{id}",
            cancellationToken);
    }

    public async Task<ApiResponse<Guid>?> CreateLoanProductAsync(
        CreateLoanProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/loan-products",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(
            cancellationToken);
    }

    public async Task<ApiResponse<string>?> SubmitForApprovalAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/loan-products/{id}/submit-for-approval",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }

    public async Task<ApiResponse<string>?> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"api/loan-products/{id}/approve",
            content: null,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(
            cancellationToken);
    }
}
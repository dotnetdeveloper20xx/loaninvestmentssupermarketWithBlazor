using System.Net.Http.Json;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Payments;

namespace LoanSuperMarket.Blazor.Services.ApiClients;

public sealed class PaymentsApiClient
{
    private readonly HttpClient _httpClient;

    public PaymentsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<PaymentResultDto>?> RecordPaymentAsync(
        Guid scheduleId,
        decimal amount,
        DateTime paymentDate,
        CancellationToken cancellationToken = default)
    {
        var request = new RecordPaymentRequest
        {
            Amount = amount,
            PaymentDate = paymentDate
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/payments/{scheduleId}/pay",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentResultDto>>(
            cancellationToken);
    }

    public async Task<ApiResponse<RepaymentScheduleDto>?> GetRepaymentScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<RepaymentScheduleDto>>(
            $"api/payments/{scheduleId}",
            cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>?> GetPaymentHistoryAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<PaymentHistoryItemDto>>>(
            $"api/payments/{scheduleId}/history",
            cancellationToken);
    }

    public async Task<ApiResponse<BulkPaymentResultDto>?> RecordBulkPaymentAsync(
        Guid scheduleId,
        decimal amount,
        DateTime paymentDate,
        CancellationToken cancellationToken = default)
    {
        var request = new RecordPaymentRequest
        {
            Amount = amount,
            PaymentDate = paymentDate
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/payments/{scheduleId}/pay-bulk",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<ApiResponse<BulkPaymentResultDto>>(
            cancellationToken);
    }
}

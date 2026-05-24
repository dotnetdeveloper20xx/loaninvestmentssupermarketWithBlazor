using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace LoanSuperMarket.Blazor.Services;

/// <summary>
/// Client-side SignalR connection to the LoanHub.
/// Provides events for real-time notifications.
/// </summary>
public sealed class LoanHubClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public event Action? OnFundingQueueChanged;
    public event Action<Guid, decimal>? OnPaymentRecorded;
    public event Action<Guid, decimal>? OnLoanFunded;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public LoanHubClient(NavigationManager navigation)
    {
        // The hub URL is relative to the API, not the Blazor app
        // In production, this would come from configuration
        var baseUri = navigation.BaseUri.TrimEnd('/');
        _hubUrl = $"{baseUri}/hubs/loans";
    }

    public async Task StartAsync(string? accessToken)
    {
        if (_connection is not null && _connection.State != HubConnectionState.Disconnected)
            return;

        if (string.IsNullOrWhiteSpace(accessToken))
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On("FundingQueueChanged", () =>
        {
            OnFundingQueueChanged?.Invoke();
        });

        _connection.On("PaymentRecorded", (Guid scheduleId, decimal amount) =>
        {
            OnPaymentRecorded?.Invoke(scheduleId, amount);
        });

        _connection.On("LoanFunded", (Guid applicationId, decimal amount) =>
        {
            OnLoanFunded?.Invoke(applicationId, amount);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            // Connection failed — non-critical, app works without real-time
            // This happens when the API isn't running or the hub URL is wrong
        }
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.StopAsync();
            }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch { }

            _connection = null;
        }
    }
}

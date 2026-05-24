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
        var baseUri = navigation.BaseUri.TrimEnd('/');
        _hubUrl = $"{baseUri}/hubs/loans";
    }

    public async Task StartAsync(string accessToken)
    {
        if (_connection is not null) return;

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

        _connection.On<object>("PaymentRecorded", (data) =>
        {
            // Parse the anonymous object
            OnPaymentRecorded?.Invoke(Guid.Empty, 0);
        });

        _connection.On<object>("LoanFunded", (data) =>
        {
            OnLoanFunded?.Invoke(Guid.Empty, 0);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            // Connection failed — non-critical, app works without real-time
        }
    }

    public async Task StopAsync()
    {
        if (_connection is not null)
        {
            await _connection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

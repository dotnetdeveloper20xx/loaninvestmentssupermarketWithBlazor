using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LoanSuperMarket.Api.Hubs;

/// <summary>
/// SignalR hub for real-time loan platform notifications.
/// Clients join groups based on their role (Lender/Borrower) and user ID.
/// </summary>
[Authorize]
public sealed class LoanHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            // Each user gets their own group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        // Role-based groups
        var user = Context.User;
        if (user?.IsInRole("Lender") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "lenders");
        }

        if (user?.IsInRole("Borrower") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "borrowers");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}

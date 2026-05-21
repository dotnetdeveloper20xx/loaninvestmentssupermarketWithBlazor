using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Queries.GetUserSessions;

/// <summary>
/// Handles the GetUserSessionsQuery by delegating to the session service.
/// </summary>
public sealed class GetUserSessionsQueryHandler
    : IRequestHandler<GetUserSessionsQuery, IReadOnlyList<UserSessionDto>>
{
    private readonly ISessionService _sessionService;

    public GetUserSessionsQueryHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<IReadOnlyList<UserSessionDto>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetUserSessionsAsync(
            request.UserId,
            cancellationToken);

        // Map from Auth.Models.UserSessionDto (record) to Users.Models.UserSessionDto (class)
        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            DeviceType = s.DeviceType,
            IpAddress = s.IpAddress,
            Browser = s.Browser,
            CreatedAtUtc = s.CreatedAtUtc,
            LastActivityAtUtc = s.LastActivityAtUtc,
            IsActive = s.IsActive
        }).ToList();
    }
}

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

public sealed class PerformanceBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        var requestName = typeof(TRequest).Name;

        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Long running request {RequestName} took {ElapsedMilliseconds} ms",
                requestName,
                elapsedMilliseconds);
        }

        return response;
    }
}
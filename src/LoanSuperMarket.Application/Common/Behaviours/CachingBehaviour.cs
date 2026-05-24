using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that caches query results.
/// Only applies to requests that implement ICacheableQuery.
/// Uses cache-aside pattern with configurable expiration.
/// </summary>
public sealed class CachingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehaviour<TRequest, TResponse>> _logger;

    public CachingBehaviour(IMemoryCache cache, ILogger<CachingBehaviour<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cacheKey = cacheableQuery.CacheKey;

        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        var response = await next(cancellationToken);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheableQuery.CacheMinutes)
        };

        _cache.Set(cacheKey, response, cacheOptions);

        return response;
    }
}

/// <summary>
/// Marker interface for queries that should be cached.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    int CacheMinutes => 5;
}

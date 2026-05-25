# 34 — Rate Limiting & Health Checks

## Overview

The platform implements rate limiting to protect against abuse and denial-of-service attacks, and health checks to enable production monitoring. Rate limiting uses ASP.NET Core's built-in `PartitionedRateLimiter` with a fixed-window strategy (100 requests per minute per IP). Health checks verify database connectivity and expose a `/health` endpoint for load balancers and monitoring tools.

---

## Feature Requirements (Plain English)

1. Limit each client IP to 100 requests per minute.
2. Queue up to 10 excess requests (oldest-first processing).
3. Return 429 Too Many Requests when the limit is exceeded.
4. Expose a `/health` endpoint that checks database connectivity.
5. Health check returns 200 (Healthy) or 503 (Unhealthy).
6. Support monitoring tools (Kubernetes probes, Azure App Service health checks).

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Rate limiting | System.Threading.RateLimiting | Fixed window per IP |
| Health checks | Microsoft.Extensions.Diagnostics.HealthChecks | Probe pattern |
| DB health | EntityFrameworkCore.HealthChecks | DbContext check |

---

## Rate Limiting Configuration

### Program.cs Setup

```csharp
// src/LoanSuperMarket.Api/Program.cs
using System.Threading.RateLimiting;

// ─── Rate Limiting Registration ───
builder.Services.AddRateLimiter(options =>
{
    // Return 429 status code when rate limit is exceeded
    options.RejectionStatusCode = 429;

    // Global limiter: applies to ALL endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partition by client IP address
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,                              // 100 requests
            Window = TimeSpan.FromMinutes(1),               // per 1 minute
            QueueLimit = 10,                                // queue up to 10 excess
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});

// ─── Middleware Pipeline (order matters) ───
app.UseRateLimiter();  // Must be before UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
```

---

## How Fixed Window Rate Limiting Works

```
Time: 0:00 ─────────────────────────── 1:00 ─────────────────────────── 2:00
      │                                  │                                  │
      │  Window 1: 100 permits           │  Window 2: 100 permits           │
      │  ┌─────────────────────────┐     │  ┌─────────────────────────┐     │
      │  │ Request 1  ✓            │     │  │ Request 1  ✓            │     │
      │  │ Request 2  ✓            │     │  │ ...                     │     │
      │  │ ...                     │     │  │ Request 100 ✓           │     │
      │  │ Request 100 ✓           │     │  │ Request 101 → queued    │     │
      │  │ Request 101 → queued    │     │  │ Request 111 → 429 ✗    │     │
      │  │ Request 110 → queued    │     │  └─────────────────────────┘     │
      │  │ Request 111 → 429 ✗    │     │                                  │
      │  └─────────────────────────┘     │                                  │
```

- **PermitLimit = 100**: Maximum 100 requests allowed per window.
- **Window = 1 minute**: The window resets every minute.
- **QueueLimit = 10**: Up to 10 excess requests are queued (processed when permits become available).
- **Beyond queue**: Returns 429 immediately.

---

## 429 Too Many Requests Response

When a client exceeds the rate limit, they receive:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Length: 0
```

The `Retry-After` header tells the client how long to wait before retrying.

### Handling 429 in Blazor

```csharp
// In AuthTokenHandler or a custom handler
if (response.StatusCode == HttpStatusCode.TooManyRequests)
{
    // Option 1: Show a toast
    var toastService = _serviceProvider.GetService<ToastService>();
    toastService?.ShowWarning("Slow down", "Too many requests. Please wait a moment.");

    // Option 2: Auto-retry after delay
    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);
    await Task.Delay(retryAfter);
    return await base.SendAsync(CloneRequest(request), cancellationToken);
}
```

---

## Advanced Rate Limiting Configurations

### Per-endpoint rate limiting

```csharp
// Different limits for different endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;           // Only 5 login attempts
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.QueueLimit = 0;            // No queuing for auth
    });

    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 10;
    });
});
```

Apply to specific endpoints:
```csharp
[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<ActionResult> Login(...) { }
```

### Sliding window (smoother distribution)

```csharp
options.AddSlidingWindowLimiter("sliding", limiterOptions =>
{
    limiterOptions.PermitLimit = 100;
    limiterOptions.Window = TimeSpan.FromMinutes(1);
    limiterOptions.SegmentsPerWindow = 4;  // 4 segments of 15 seconds each
    limiterOptions.QueueLimit = 10;
});
```

### Token bucket (burst-friendly)

```csharp
options.AddTokenBucketLimiter("burst", limiterOptions =>
{
    limiterOptions.TokenLimit = 100;           // Bucket capacity
    limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
    limiterOptions.TokensPerPeriod = 20;      // Refill 20 tokens every 10s
    limiterOptions.QueueLimit = 10;
});
```

---

## Health Check Registration

### Program.cs Setup

```csharp
// src/LoanSuperMarket.Api/Program.cs

// Register health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// Map the endpoint
app.MapHealthChecks("/health");
```

### What `AddDbContextCheck` does

It executes `context.Database.CanConnectAsync()` — a lightweight check that verifies the database connection is alive without running any queries.

---

## Health Check Response

### Healthy (200 OK)

```
GET /health
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

### Unhealthy (503 Service Unavailable)

```
GET /health
HTTP/1.1 503 Service Unavailable
Content-Type: text/plain

Unhealthy
```

### Detailed response (optional)

For more detail, configure a custom response writer:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };

        await context.Response.WriteAsJsonAsync(result);
    }
});
```

Response:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 12.5,
      "exception": null
    }
  ],
  "totalDuration": 15.2
}
```

---

## Adding Custom Health Checks

### Check external service availability

```csharp
public sealed class ExternalApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public ExternalApiHealthCheck(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/ping", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("External API is reachable.")
                : HealthCheckResult.Degraded("External API returned non-success status.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("External API is unreachable.", ex);
        }
    }
}
```

Register:
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<ExternalApiHealthCheck>("external-api");
```

### Check disk space

```csharp
public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var drive = new DriveInfo("C");
        var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);

        if (freeGb < 1)
            return Task.FromResult(HealthCheckResult.Unhealthy($"Low disk: {freeGb:N1} GB"));
        if (freeGb < 5)
            return Task.FromResult(HealthCheckResult.Degraded($"Disk space: {freeGb:N1} GB"));

        return Task.FromResult(HealthCheckResult.Healthy($"Disk space: {freeGb:N1} GB"));
    }
}
```

---

## Production Monitoring Considerations

### Kubernetes Liveness & Readiness Probes

```yaml
# kubernetes deployment.yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Separate liveness and readiness endpoints

```csharp
// Liveness: "Is the process alive?" (lightweight)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks — just confirms the process responds
});

// Readiness: "Can it serve traffic?" (includes DB check)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Registration with tags
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
```

### Azure App Service Health Check

In Azure Portal → App Service → Health Check:
- Path: `/health`
- Threshold: 3 consecutive failures before marking unhealthy
- Azure will restart the instance if unhealthy

### Monitoring dashboard integration

Health check data can be pushed to:
- **Application Insights** — via `AspNetCore.HealthChecks.Publisher.ApplicationInsights`
- **Prometheus** — via `AspNetCore.HealthChecks.Publisher.Prometheus`
- **Seq/Datadog** — via custom publishers

---

## Rate Limiting + Health Checks Together

The health check endpoint should be excluded from rate limiting:

```csharp
// Option 1: Disable rate limiting for health endpoint
app.MapHealthChecks("/health").DisableRateLimiting();

// Option 2: Use a policy that excludes health checks
options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
{
    // Don't rate-limit health checks
    if (context.Request.Path.StartsWithSegments("/health"))
        return RateLimitPartition.GetNoLimiter("health");

    var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    return RateLimitPartition.GetFixedWindowLimiter(clientId, ...);
});
```

---

## Step-by-Step Extension Guide

### Adding rate limiting to a specific controller

```csharp
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]  // Apply the "auth" policy
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult> Login(...) { }
}
```

### Adding a new health check

1. Create a class implementing `IHealthCheck`
2. Register with `builder.Services.AddHealthChecks().AddCheck<MyCheck>("name")`
3. Optionally tag it for selective endpoint mapping

### Configuring rate limits from appsettings

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowMinutes": 1,
    "QueueLimit": 10
  }
}
```

```csharp
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
var windowMinutes = rateLimitConfig.GetValue<int>("WindowMinutes", 1);
var queueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(windowMinutes),
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
});
```

---

## Testing Considerations

### Rate limiting tests

```csharp
[Fact]
public async Task Should_Return_429_When_Rate_Limit_Exceeded()
{
    var client = _factory.CreateClient();

    // Send 101 requests rapidly
    for (int i = 0; i < 101; i++)
    {
        var response = await client.GetAsync("/api/loan-products");

        if (i < 100)
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // The 111th request (after queue) should be 429
    var lastResponse = await client.GetAsync("/api/loan-products");
    Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
}
```

### Health check tests

```csharp
[Fact]
public async Task Health_Endpoint_Returns_Healthy()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/health");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## Common Pitfalls

1. **Rate limiting behind a reverse proxy** — `RemoteIpAddress` will be the proxy's IP, not the client's. Use `X-Forwarded-For` header:
   ```csharp
   var clientId = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
       ?? context.Connection.RemoteIpAddress?.ToString()
       ?? "anonymous";
   ```

2. **Health check timeout** — If the database is slow, the health check might timeout. Set a timeout:
   ```csharp
   .AddDbContextCheck<ApplicationDbContext>("database",
       timeout: TimeSpan.FromSeconds(5));
   ```

3. **Rate limiting the health endpoint** — Load balancers poll `/health` frequently. Exclude it from rate limiting.

4. **Authenticated health checks** — Don't require authentication for health checks. Load balancers can't authenticate.

5. **Rate limit too aggressive** — 100/min might be too low for SPAs that make many parallel requests on page load. Monitor and adjust.

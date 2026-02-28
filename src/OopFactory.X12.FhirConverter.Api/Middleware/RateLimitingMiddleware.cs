using System.Collections.Concurrent;
using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Per-tenant rate limiting using a sliding window counter.
/// Rate limits are determined by tenant tier configuration.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, TenantRateTracker> _trackers = new();

    // Cleanup stale trackers every 10 minutes
    private DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Skip rate limiting if no tenant context (unauthenticated paths)
        if (string.IsNullOrEmpty(tenantContext.TenantId))
        {
            await _next(context);
            return;
        }

        CleanupStaleTrackers();

        var tracker = _trackers.GetOrAdd(tenantContext.TenantId,
            _ => new TenantRateTracker(tenantContext.RateLimitPerHour));

        // Update limit in case tenant tier changed
        tracker.HourlyLimit = tenantContext.RateLimitPerHour;

        if (!tracker.TryConsume())
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId} ({OrgName}). Limit: {Limit}/hr",
                tenantContext.TenantId, tenantContext.OrganizationName, tenantContext.RateLimitPerHour);

            var retryAfterSeconds = tracker.SecondsUntilNextWindow();

            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = tenantContext.RateLimitPerHour.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";

            await context.Response.WriteAsJsonAsync(new
            {
                resourceType = "OperationOutcome",
                issue = new[]
                {
                    new
                    {
                        severity = "error",
                        code = "throttled",
                        diagnostics = $"Rate limit exceeded. Maximum {tenantContext.RateLimitPerHour} requests per hour. Retry after {retryAfterSeconds} seconds."
                    }
                }
            });
            return;
        }

        // Add rate limit headers to successful responses
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = tenantContext.RateLimitPerHour.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = tracker.Remaining.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void CleanupStaleTrackers()
    {
        if (DateTime.UtcNow - _lastCleanup < CleanupInterval) return;
        _lastCleanup = DateTime.UtcNow;

        var staleKeys = _trackers
            .Where(kv => kv.Value.IsStale())
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in staleKeys)
            _trackers.TryRemove(key, out _);
    }
}

/// <summary>
/// Sliding window rate counter for a single tenant.
/// </summary>
internal class TenantRateTracker
{
    private readonly object _lock = new();
    private readonly Queue<DateTime> _requests = new();
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public int HourlyLimit { get; set; }
    public int Remaining => Math.Max(0, HourlyLimit - CurrentCount);
    private DateTime _lastAccess = DateTime.UtcNow;

    public TenantRateTracker(int hourlyLimit)
    {
        HourlyLimit = hourlyLimit;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            _lastAccess = DateTime.UtcNow;
            PruneExpired();

            if (_requests.Count >= HourlyLimit)
                return false;

            _requests.Enqueue(DateTime.UtcNow);
            return true;
        }
    }

    public int SecondsUntilNextWindow()
    {
        lock (_lock)
        {
            if (_requests.Count == 0) return 0;
            var oldest = _requests.Peek();
            var expiresAt = oldest + Window;
            var remaining = expiresAt - DateTime.UtcNow;
            return Math.Max(1, (int)remaining.TotalSeconds);
        }
    }

    private int CurrentCount
    {
        get
        {
            lock (_lock)
            {
                PruneExpired();
                return _requests.Count;
            }
        }
    }

    public bool IsStale() => DateTime.UtcNow - _lastAccess > TimeSpan.FromHours(2);

    private void PruneExpired()
    {
        var cutoff = DateTime.UtcNow - Window;
        while (_requests.Count > 0 && _requests.Peek() < cutoff)
            _requests.Dequeue();
    }
}

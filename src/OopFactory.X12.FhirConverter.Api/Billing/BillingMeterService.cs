using System.Collections.Concurrent;

namespace OopFactory.X12.FhirConverter.Api.Billing;

/// <summary>
/// In-memory billing meter for local development. In production, this writes to
/// DynamoDB TransactionLog table and emits CloudWatch custom metrics.
/// </summary>
public class BillingMeterService : IBillingMeterService
{
    private readonly ConcurrentBag<BillingEvent> _events = new();
    private readonly ILogger<BillingMeterService> _logger;

    public BillingMeterService(ILogger<BillingMeterService> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(BillingEvent billingEvent)
    {
        _events.Add(billingEvent);

        _logger.LogInformation(
            "Billing: Tenant={TenantId} Type={TransactionType} Direction={Direction} Status={Status} Latency={LatencyMs}ms Bytes={BytesIn}/{BytesOut}",
            billingEvent.TenantId,
            billingEvent.TransactionType,
            billingEvent.Direction,
            billingEvent.Status,
            billingEvent.LatencyMs,
            billingEvent.BytesIn,
            billingEvent.BytesOut);

        // In production: emit CloudWatch metrics here
        // await _cloudWatch.PutMetricDataAsync(new PutMetricDataRequest { ... });

        return Task.CompletedTask;
    }

    public Task<UsageSummary> GetUsageAsync(string tenantId, DateTime from, DateTime to)
    {
        var tenantEvents = _events
            .Where(e => e.TenantId == tenantId && e.Timestamp >= from && e.Timestamp <= to)
            .ToList();

        var summary = BuildSummary(tenantId, "", from, to, tenantEvents);
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<UsageSummary>> GetAllTenantsUsageAsync(DateTime from, DateTime to)
    {
        var summaries = _events
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .GroupBy(e => e.TenantId)
            .Select(g => BuildSummary(g.Key, "", from, to, g.ToList()))
            .ToList() as IReadOnlyList<UsageSummary>;

        return Task.FromResult(summaries);
    }

    public Task<IReadOnlyList<BillingEvent>> GetTransactionLogAsync(string tenantId, DateTime from, DateTime to, int limit = 100)
    {
        var events = _events
            .Where(e => e.TenantId == tenantId && e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList() as IReadOnlyList<BillingEvent>;

        return Task.FromResult(events);
    }

    private static UsageSummary BuildSummary(string tenantId, string orgName, DateTime from, DateTime to, List<BillingEvent> events)
    {
        var latencies = events.Where(e => e.Status == TransactionStatus.Success).Select(e => e.LatencyMs).OrderBy(l => l).ToList();
        var p95Index = latencies.Count > 0 ? (int)(latencies.Count * 0.95) : 0;

        return new UsageSummary
        {
            TenantId = tenantId,
            OrganizationName = orgName,
            PeriodStart = from,
            PeriodEnd = to,
            TotalTransactions = events.Count,
            SuccessfulTransactions = events.Count(e => e.Status == TransactionStatus.Success),
            FailedTransactions = events.Count(e => e.Status == TransactionStatus.Failed),
            TotalBytesProcessed = events.Sum(e => e.BytesIn + e.BytesOut),
            AverageLatencyMs = latencies.Count > 0 ? latencies.Average() : 0,
            P95LatencyMs = latencies.Count > 0 ? latencies[Math.Min(p95Index, latencies.Count - 1)] : 0,
            TransactionsByType = events.GroupBy(e => e.TransactionType).ToDictionary(g => g.Key, g => (long)g.Count()),
            TransactionsByDirection = events.GroupBy(e => e.Direction.ToString()).ToDictionary(g => g.Key, g => (long)g.Count())
        };
    }
}

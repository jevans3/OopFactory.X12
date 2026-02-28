using System.Collections.Concurrent;

namespace OopFactory.X12.FhirConverter.Api.Compliance;

/// <summary>
/// In-memory audit log for local development.
/// In production, writes to DynamoDB AuditLog table with 6-year TTL (HIPAA minimum).
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ConcurrentBag<AuditLogEntry> _entries = new();
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ILogger<AuditLogService> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(AuditLogEntry entry)
    {
        _entries.Add(entry);

        _logger.LogInformation(
            "AUDIT: Tenant={TenantId} Action={Action} Endpoint={Endpoint} Status={HttpStatusCode} IP={SourceIp} PHI={PhiAccessLevel} CorrelationId={CorrelationId}",
            entry.TenantId,
            entry.Action,
            entry.Endpoint,
            entry.HttpStatusCode,
            entry.SourceIp,
            entry.PhiAccessLevel,
            entry.CorrelationId);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLogEntry>> QueryAsync(string tenantId, DateTime from, DateTime to, int limit = 100)
    {
        var entries = _entries
            .Where(e => e.TenantId == tenantId && e.Timestamp >= from && e.Timestamp <= to)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList() as IReadOnlyList<AuditLogEntry>;

        return Task.FromResult(entries);
    }
}

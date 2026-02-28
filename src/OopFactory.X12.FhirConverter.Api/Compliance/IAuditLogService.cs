namespace OopFactory.X12.FhirConverter.Api.Compliance;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntry entry);
    Task<IReadOnlyList<AuditLogEntry>> QueryAsync(string tenantId, DateTime from, DateTime to, int limit = 100);
}

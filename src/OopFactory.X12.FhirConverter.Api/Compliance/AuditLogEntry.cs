namespace OopFactory.X12.FhirConverter.Api.Compliance;

/// <summary>
/// Structured audit log entry for HIPAA compliance.
/// Records WHO accessed WHAT, WHEN, from WHERE.
/// </summary>
public class AuditLogEntry
{
    public string EntryId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string HttpMethod { get; set; } = "";
    public int HttpStatusCode { get; set; }
    public string SourceIp { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public long RequestSizeBytes { get; set; }
    public long ResponseSizeBytes { get; set; }
    public string CorrelationId { get; set; } = "";
    public PhiAccessLevel PhiAccessLevel { get; set; } = PhiAccessLevel.None;
    public long LatencyMs { get; set; }
}

public enum PhiAccessLevel
{
    None,
    Masked,
    Full
}

namespace OopFactory.X12.FhirConverter.Api.Billing;

/// <summary>
/// Represents a single billable transaction event.
/// Captured for every API call for usage-based billing.
/// </summary>
public class BillingEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = "";
    public ConversionDirection Direction { get; set; }
    public TransactionStatus Status { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public long LatencyMs { get; set; }
    public string Endpoint { get; set; } = "";
    public int HttpStatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string CorrelationId { get; set; } = "";
}

public enum ConversionDirection
{
    X12ToFhir,
    FhirToX12
}

public enum TransactionStatus
{
    Success,
    Failed,
    Partial
}

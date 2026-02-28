namespace OopFactory.X12.FhirConverter.Api.Billing;

public interface IBillingMeterService
{
    Task RecordAsync(BillingEvent billingEvent);
    Task<UsageSummary> GetUsageAsync(string tenantId, DateTime from, DateTime to);
    Task<IReadOnlyList<UsageSummary>> GetAllTenantsUsageAsync(DateTime from, DateTime to);
    Task<IReadOnlyList<BillingEvent>> GetTransactionLogAsync(string tenantId, DateTime from, DateTime to, int limit = 100);
}

public class UsageSummary
{
    public string TenantId { get; set; } = "";
    public string OrganizationName { get; set; } = "";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public long TotalTransactions { get; set; }
    public long SuccessfulTransactions { get; set; }
    public long FailedTransactions { get; set; }
    public long TotalBytesProcessed { get; set; }
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public Dictionary<string, long> TransactionsByType { get; set; } = new();
    public Dictionary<string, long> TransactionsByDirection { get; set; } = new();
}

namespace OopFactory.X12.FhirConverter.Api.Configuration;

public class FhirConverterOptions
{
    public string TemplatePath { get; set; } = "Templates";
    public string DefaultX12Version { get; set; } = "5010";
    public bool ValidateFhirOutput { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public string[] SupportedTransactions { get; set; } = ["278", "275", "270", "271", "276", "277", "837", "835"];
    public Dictionary<string, string> PayerEndpoints { get; set; } = new();

    // Authentication
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
    public bool RequireAuthentication { get; set; } = true;

    // AWS (for production)
    public string? AwsRegion { get; set; }
    public string? TenantTableName { get; set; } = "X12FhirConverter-Tenants";
    public string? TransactionLogTableName { get; set; } = "X12FhirConverter-TransactionLog";
    public string? AuditLogTableName { get; set; } = "X12FhirConverter-AuditLog";
    public string? BillingSqsQueueUrl { get; set; }
}

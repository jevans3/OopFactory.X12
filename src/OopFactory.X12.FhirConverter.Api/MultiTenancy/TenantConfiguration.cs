namespace OopFactory.X12.FhirConverter.Api.MultiTenancy;

/// <summary>
/// Represents a tenant record as stored in DynamoDB or in-memory for local dev.
/// </summary>
public class TenantConfiguration
{
    public string TenantId { get; set; } = "";
    public string ApiKeyHash { get; set; } = "";
    public string OrganizationName { get; set; } = "";
    public TenantTier Tier { get; set; } = TenantTier.Developer;
    public bool IsActive { get; set; } = true;
    public string[] AllowedTransactionTypes { get; set; } = ["278", "275", "270", "271", "276", "277", "837", "835"];
    public int RateLimitPerHour { get; set; } = 100;
    public string? ContactEmail { get; set; }
    public Dictionary<string, string> PayerEndpoints { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SuspendedAt { get; set; }
}

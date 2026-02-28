namespace OopFactory.X12.FhirConverter.Api.MultiTenancy;

/// <summary>
/// Scoped service holding tenant identity for the current request.
/// Populated by TenantContextMiddleware after successful authentication.
/// </summary>
public class TenantContext
{
    public string TenantId { get; set; } = "";
    public string OrganizationName { get; set; } = "";
    public TenantTier Tier { get; set; } = TenantTier.Developer;
    public string[] AllowedTransactionTypes { get; set; } = [];
    public int RateLimitPerHour { get; set; } = 100;
    public bool IsActive { get; set; }
    public string? ContactEmail { get; set; }
    public Dictionary<string, string> PayerEndpoints { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum TenantTier
{
    Developer = 0,
    Standard = 1,
    Enterprise = 2
}

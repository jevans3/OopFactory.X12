namespace OopFactory.X12.FhirConverter.Api.MultiTenancy;

public interface ITenantService
{
    Task<TenantConfiguration?> ValidateApiKeyAsync(string apiKey);
    Task<TenantConfiguration?> GetTenantAsync(string tenantId);
    Task<TenantConfiguration> OnboardTenantAsync(CreateTenantRequest request);
    Task SuspendTenantAsync(string tenantId);
    Task ReactivateTenantAsync(string tenantId);
    Task DeleteTenantAsync(string tenantId);
    Task<IReadOnlyList<TenantConfiguration>> ListTenantsAsync();
    Task UpdateTenantAsync(string tenantId, UpdateTenantRequest request);
}

public class CreateTenantRequest
{
    public string OrganizationName { get; set; } = "";
    public TenantTier Tier { get; set; } = TenantTier.Developer;
    public string[] AllowedTransactionTypes { get; set; } = ["278", "275", "270", "271", "276", "277", "837", "835"];
    public int? RateLimitPerHour { get; set; }
    public string? ContactEmail { get; set; }
    public Dictionary<string, string>? PayerEndpoints { get; set; }
}

public class UpdateTenantRequest
{
    public string? OrganizationName { get; set; }
    public TenantTier? Tier { get; set; }
    public string[]? AllowedTransactionTypes { get; set; }
    public int? RateLimitPerHour { get; set; }
    public string? ContactEmail { get; set; }
    public Dictionary<string, string>? PayerEndpoints { get; set; }
}

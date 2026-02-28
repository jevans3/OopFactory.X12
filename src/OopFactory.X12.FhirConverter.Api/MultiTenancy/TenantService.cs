using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace OopFactory.X12.FhirConverter.Api.MultiTenancy;

/// <summary>
/// Tenant management service. Uses in-memory store for local development
/// and DynamoDB for production (controlled by configuration).
/// </summary>
public class TenantService : ITenantService
{
    private readonly ConcurrentDictionary<string, TenantConfiguration> _tenants = new();
    private readonly ConcurrentDictionary<string, string> _apiKeyToTenantId = new();
    private readonly ILogger<TenantService> _logger;

    public TenantService(ILogger<TenantService> logger, IConfiguration configuration)
    {
        _logger = logger;
        SeedDefaultTenants(configuration);
    }

    public Task<TenantConfiguration?> ValidateApiKeyAsync(string apiKey)
    {
        var keyHash = HashApiKey(apiKey);

        if (_apiKeyToTenantId.TryGetValue(keyHash, out var tenantId)
            && _tenants.TryGetValue(tenantId, out var tenant)
            && tenant.IsActive)
        {
            return Task.FromResult<TenantConfiguration?>(tenant);
        }

        return Task.FromResult<TenantConfiguration?>(null);
    }

    public Task<TenantConfiguration?> GetTenantAsync(string tenantId)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task<TenantConfiguration> OnboardTenantAsync(CreateTenantRequest request)
    {
        var tenantId = $"tenant_{Guid.NewGuid():N}";
        var apiKey = GenerateApiKey();
        var keyHash = HashApiKey(apiKey);

        var rateLimits = GetDefaultRateLimit(request.Tier);

        var tenant = new TenantConfiguration
        {
            TenantId = tenantId,
            ApiKeyHash = keyHash,
            OrganizationName = request.OrganizationName,
            Tier = request.Tier,
            IsActive = true,
            AllowedTransactionTypes = request.AllowedTransactionTypes,
            RateLimitPerHour = request.RateLimitPerHour ?? rateLimits,
            ContactEmail = request.ContactEmail,
            PayerEndpoints = request.PayerEndpoints ?? new(),
            CreatedAt = DateTime.UtcNow
        };

        _tenants[tenantId] = tenant;
        _apiKeyToTenantId[keyHash] = tenantId;

        _logger.LogInformation("Onboarded tenant {TenantId} ({OrgName}) at tier {Tier}",
            tenantId, request.OrganizationName, request.Tier);

        // Return with the plaintext API key (only time it's available)
        tenant.Metadata["apiKey"] = apiKey;
        return Task.FromResult(tenant);
    }

    public Task SuspendTenantAsync(string tenantId)
    {
        if (_tenants.TryGetValue(tenantId, out var tenant))
        {
            tenant.IsActive = false;
            tenant.SuspendedAt = DateTime.UtcNow;
            _logger.LogWarning("Suspended tenant {TenantId} ({OrgName})", tenantId, tenant.OrganizationName);
        }
        return Task.CompletedTask;
    }

    public Task ReactivateTenantAsync(string tenantId)
    {
        if (_tenants.TryGetValue(tenantId, out var tenant))
        {
            tenant.IsActive = true;
            tenant.SuspendedAt = null;
            _logger.LogInformation("Reactivated tenant {TenantId} ({OrgName})", tenantId, tenant.OrganizationName);
        }
        return Task.CompletedTask;
    }

    public Task DeleteTenantAsync(string tenantId)
    {
        if (_tenants.TryRemove(tenantId, out var tenant))
        {
            // Remove the API key mapping
            var keyToRemove = _apiKeyToTenantId.FirstOrDefault(kv => kv.Value == tenantId).Key;
            if (keyToRemove != null)
                _apiKeyToTenantId.TryRemove(keyToRemove, out _);

            _logger.LogWarning("Deleted tenant {TenantId} ({OrgName})", tenantId, tenant.OrganizationName);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TenantConfiguration>> ListTenantsAsync()
    {
        var list = _tenants.Values.ToList() as IReadOnlyList<TenantConfiguration>;
        return Task.FromResult(list);
    }

    public Task UpdateTenantAsync(string tenantId, UpdateTenantRequest request)
    {
        if (!_tenants.TryGetValue(tenantId, out var tenant))
            throw new KeyNotFoundException($"Tenant {tenantId} not found");

        if (request.OrganizationName != null) tenant.OrganizationName = request.OrganizationName;
        if (request.Tier.HasValue) tenant.Tier = request.Tier.Value;
        if (request.AllowedTransactionTypes != null) tenant.AllowedTransactionTypes = request.AllowedTransactionTypes;
        if (request.RateLimitPerHour.HasValue) tenant.RateLimitPerHour = request.RateLimitPerHour.Value;
        if (request.ContactEmail != null) tenant.ContactEmail = request.ContactEmail;
        if (request.PayerEndpoints != null) tenant.PayerEndpoints = request.PayerEndpoints;

        _logger.LogInformation("Updated tenant {TenantId} ({OrgName})", tenantId, tenant.OrganizationName);
        return Task.CompletedTask;
    }

    private void SeedDefaultTenants(IConfiguration configuration)
    {
        // Seed tenants from configuration for local development
        var seedTenants = configuration.GetSection("Tenants").GetChildren();
        foreach (var tenantSection in seedTenants)
        {
            var tenantId = tenantSection["TenantId"] ?? $"tenant_{Guid.NewGuid():N}";
            var apiKey = tenantSection["ApiKey"] ?? "";
            var keyHash = HashApiKey(apiKey);

            var tenant = new TenantConfiguration
            {
                TenantId = tenantId,
                ApiKeyHash = keyHash,
                OrganizationName = tenantSection["OrganizationName"] ?? "Unknown",
                Tier = Enum.TryParse<TenantTier>(tenantSection["Tier"], out var tier) ? tier : TenantTier.Developer,
                IsActive = true,
                AllowedTransactionTypes = tenantSection.GetSection("AllowedTransactionTypes").Get<string[]>()
                    ?? ["278", "275", "270", "271", "276", "277", "837", "835"],
                RateLimitPerHour = int.TryParse(tenantSection["RateLimitPerHour"], out var limit) ? limit : 100,
                ContactEmail = tenantSection["ContactEmail"],
                CreatedAt = DateTime.UtcNow
            };

            _tenants[tenantId] = tenant;
            if (!string.IsNullOrEmpty(apiKey))
                _apiKeyToTenantId[keyHash] = tenantId;
        }

        // Always seed a default dev tenant if no tenants configured
        if (_tenants.IsEmpty)
        {
            var devApiKey = "dev-api-key-12345";
            var devKeyHash = HashApiKey(devApiKey);
            var devTenant = new TenantConfiguration
            {
                TenantId = "tenant_dev",
                ApiKeyHash = devKeyHash,
                OrganizationName = "Development Tenant",
                Tier = TenantTier.Enterprise,
                IsActive = true,
                RateLimitPerHour = 100000,
                CreatedAt = DateTime.UtcNow
            };
            _tenants["tenant_dev"] = devTenant;
            _apiKeyToTenantId[devKeyHash] = "tenant_dev";
            _logger.LogInformation("Seeded default development tenant (API key: {ApiKey})", devApiKey);
        }
    }

    private static int GetDefaultRateLimit(TenantTier tier) => tier switch
    {
        TenantTier.Developer => 100,
        TenantTier.Standard => 10_000,
        TenantTier.Enterprise => 1_000_000,
        _ => 100
    };

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"x12fhir_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
    }

    internal static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

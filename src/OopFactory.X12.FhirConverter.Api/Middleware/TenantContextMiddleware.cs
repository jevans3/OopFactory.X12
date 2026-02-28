using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Populates the scoped TenantContext from the validated tenant config
/// stored by ApiKeyAuthMiddleware. Enforces transaction type restrictions.
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.Items.TryGetValue("TenantConfig", out var configObj)
            && configObj is TenantConfiguration config)
        {
            tenantContext.TenantId = config.TenantId;
            tenantContext.OrganizationName = config.OrganizationName;
            tenantContext.Tier = config.Tier;
            tenantContext.AllowedTransactionTypes = config.AllowedTransactionTypes;
            tenantContext.RateLimitPerHour = config.RateLimitPerHour;
            tenantContext.IsActive = config.IsActive;
            tenantContext.ContactEmail = config.ContactEmail;
            tenantContext.PayerEndpoints = config.PayerEndpoints;
            tenantContext.Metadata = config.Metadata;
        }

        await _next(context);
    }
}

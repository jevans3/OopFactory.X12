using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Administrative endpoints for tenant management.
/// In production, these should be protected by a separate admin API key or IAM role.
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
public class AdminTenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public AdminTenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// List all tenants.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListTenants()
    {
        var tenants = await _tenantService.ListTenantsAsync();
        return Ok(new
        {
            count = tenants.Count,
            tenants = tenants.Select(t => new
            {
                t.TenantId,
                t.OrganizationName,
                tier = t.Tier.ToString(),
                t.IsActive,
                t.AllowedTransactionTypes,
                t.RateLimitPerHour,
                t.ContactEmail,
                t.CreatedAt,
                t.SuspendedAt
            })
        });
    }

    /// <summary>
    /// Get a specific tenant.
    /// </summary>
    [HttpGet("{tenantId}")]
    public async Task<IActionResult> GetTenant(string tenantId)
    {
        var tenant = await _tenantService.GetTenantAsync(tenantId);
        if (tenant == null)
            return NotFound(new { error = $"Tenant {tenantId} not found" });

        return Ok(new
        {
            tenant.TenantId,
            tenant.OrganizationName,
            tier = tenant.Tier.ToString(),
            tenant.IsActive,
            tenant.AllowedTransactionTypes,
            tenant.RateLimitPerHour,
            tenant.ContactEmail,
            tenant.PayerEndpoints,
            tenant.CreatedAt,
            tenant.SuspendedAt
        });
    }

    /// <summary>
    /// Onboard a new tenant. Returns the API key (only time it's visible).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> OnboardTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return BadRequest(new { error = "OrganizationName is required" });

        var tenant = await _tenantService.OnboardTenantAsync(request);

        return Created($"/api/admin/tenants/{tenant.TenantId}", new
        {
            tenant.TenantId,
            tenant.OrganizationName,
            tier = tenant.Tier.ToString(),
            apiKey = tenant.Metadata.GetValueOrDefault("apiKey"),
            tenant.AllowedTransactionTypes,
            tenant.RateLimitPerHour,
            message = "IMPORTANT: Save the API key now. It cannot be retrieved later."
        });
    }

    /// <summary>
    /// Update a tenant's configuration.
    /// </summary>
    [HttpPatch("{tenantId}")]
    public async Task<IActionResult> UpdateTenant(string tenantId, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            await _tenantService.UpdateTenantAsync(tenantId, request);
            return Ok(new { message = $"Tenant {tenantId} updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Tenant {tenantId} not found" });
        }
    }

    /// <summary>
    /// Suspend a tenant (disable API access).
    /// </summary>
    [HttpPost("{tenantId}/suspend")]
    public async Task<IActionResult> SuspendTenant(string tenantId)
    {
        await _tenantService.SuspendTenantAsync(tenantId);
        return Ok(new { message = $"Tenant {tenantId} suspended" });
    }

    /// <summary>
    /// Reactivate a suspended tenant.
    /// </summary>
    [HttpPost("{tenantId}/reactivate")]
    public async Task<IActionResult> ReactivateTenant(string tenantId)
    {
        await _tenantService.ReactivateTenantAsync(tenantId);
        return Ok(new { message = $"Tenant {tenantId} reactivated" });
    }

    /// <summary>
    /// Delete a tenant permanently.
    /// </summary>
    [HttpDelete("{tenantId}")]
    public async Task<IActionResult> DeleteTenant(string tenantId)
    {
        await _tenantService.DeleteTenantAsync(tenantId);
        return Ok(new { message = $"Tenant {tenantId} deleted" });
    }
}

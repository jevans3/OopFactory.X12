using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Billing;
using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Tenant-facing usage endpoint. Uses the authenticated tenant's API key
/// to show their own usage metrics (no admin access needed).
/// </summary>
[ApiController]
[Route("api/usage")]
public class TenantUsageController : ControllerBase
{
    private readonly IBillingMeterService _billingMeter;
    private readonly TenantContext _tenantContext;

    public TenantUsageController(IBillingMeterService billingMeter, TenantContext tenantContext)
    {
        _billingMeter = billingMeter;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get your own usage for the current billing period.
    /// Authenticated via X-Api-Key header (shows your tenant's usage only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyUsage(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrEmpty(_tenantContext.TenantId))
            return Unauthorized(new { error = "Authentication required" });

        var periodStart = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var periodEnd = to ?? DateTime.UtcNow;

        var summary = await _billingMeter.GetUsageAsync(_tenantContext.TenantId, periodStart, periodEnd);

        return Ok(new
        {
            tenantId = _tenantContext.TenantId,
            organizationName = _tenantContext.OrganizationName,
            tier = _tenantContext.Tier.ToString(),
            period = new { start = periodStart, end = periodEnd },
            summary.TotalTransactions,
            summary.SuccessfulTransactions,
            summary.FailedTransactions,
            summary.AverageLatencyMs,
            byType = summary.TransactionsByType,
            byDirection = summary.TransactionsByDirection
        });
    }
}

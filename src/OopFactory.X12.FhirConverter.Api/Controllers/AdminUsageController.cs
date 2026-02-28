using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Billing;
using OopFactory.X12.FhirConverter.Api.Compliance;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Administrative endpoints for usage metrics and billing telemetry.
/// Provides transaction counts, summaries, and audit trail access for billing teams.
/// </summary>
[ApiController]
[Route("api/admin/usage")]
public class AdminUsageController : ControllerBase
{
    private readonly IBillingMeterService _billingMeter;
    private readonly IAuditLogService _auditLog;

    public AdminUsageController(IBillingMeterService billingMeter, IAuditLogService auditLog)
    {
        _billingMeter = billingMeter;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Get usage summary for a specific tenant.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenantUsage(
        [FromQuery] string tenantId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { error = "tenantId query parameter is required" });

        var periodStart = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var periodEnd = to ?? DateTime.UtcNow;

        var summary = await _billingMeter.GetUsageAsync(tenantId, periodStart, periodEnd);
        return Ok(summary);
    }

    /// <summary>
    /// Get usage summary for all tenants (for billing team).
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetAllTenantsUsage(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var periodStart = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var periodEnd = to ?? DateTime.UtcNow;

        var summaries = await _billingMeter.GetAllTenantsUsageAsync(periodStart, periodEnd);
        return Ok(new
        {
            period = new { start = periodStart, end = periodEnd },
            tenantCount = summaries.Count,
            totalTransactions = summaries.Sum(s => s.TotalTransactions),
            tenants = summaries
        });
    }

    /// <summary>
    /// Get detailed transaction log for a tenant.
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactionLog(
        [FromQuery] string tenantId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { error = "tenantId query parameter is required" });

        var periodStart = from ?? DateTime.UtcNow.AddDays(-7);
        var periodEnd = to ?? DateTime.UtcNow;

        var events = await _billingMeter.GetTransactionLogAsync(tenantId, periodStart, periodEnd, limit);
        return Ok(new
        {
            tenantId,
            count = events.Count,
            transactions = events
        });
    }

    /// <summary>
    /// Get audit trail for a tenant (HIPAA compliance).
    /// </summary>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string tenantId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { error = "tenantId query parameter is required" });

        var periodStart = from ?? DateTime.UtcNow.AddDays(-30);
        var periodEnd = to ?? DateTime.UtcNow;

        var entries = await _auditLog.QueryAsync(tenantId, periodStart, periodEnd, limit);
        return Ok(new
        {
            tenantId,
            count = entries.Count,
            auditEntries = entries
        });
    }
}

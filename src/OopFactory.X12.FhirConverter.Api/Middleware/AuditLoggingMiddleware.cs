using System.Diagnostics;
using OopFactory.X12.FhirConverter.Api.Billing;
using OopFactory.X12.FhirConverter.Api.Compliance;
using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Captures audit log entries and billing events for every API request.
/// Runs after authentication and tenant context are established.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        IAuditLogService auditLogService,
        IBillingMeterService billingMeter)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // Enable request body buffering so we can measure size
        context.Request.EnableBuffering();
        var requestSize = context.Request.ContentLength ?? 0;

        // Wrap the response body to capture its size
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Copy the response body back
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            var responseSize = responseBodyStream.Length;
            var endpoint = context.Request.Path.Value ?? "";
            var transactionType = InferTransactionType(endpoint);

            // Write audit log entry
            if (!string.IsNullOrEmpty(tenantContext.TenantId))
            {
                var auditEntry = new AuditLogEntry
                {
                    TenantId = tenantContext.TenantId,
                    Action = InferAction(endpoint, context.Request.Method),
                    TransactionType = transactionType,
                    Endpoint = endpoint,
                    HttpMethod = context.Request.Method,
                    HttpStatusCode = context.Response.StatusCode,
                    SourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    RequestSizeBytes = requestSize,
                    ResponseSizeBytes = responseSize,
                    CorrelationId = correlationId,
                    PhiAccessLevel = DeterminePhiAccess(endpoint),
                    LatencyMs = stopwatch.ElapsedMilliseconds
                };
                await auditLogService.WriteAsync(auditEntry);

                // Record billing event
                var billingEvent = new BillingEvent
                {
                    TenantId = tenantContext.TenantId,
                    TransactionType = transactionType,
                    Direction = InferDirection(endpoint),
                    Status = context.Response.StatusCode < 400 ? TransactionStatus.Success : TransactionStatus.Failed,
                    BytesIn = requestSize,
                    BytesOut = responseSize,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    Endpoint = endpoint,
                    HttpStatusCode = context.Response.StatusCode,
                    CorrelationId = correlationId
                };
                await billingMeter.RecordAsync(billingEvent);
            }
        }
    }

    private static string InferAction(string endpoint, string method)
    {
        if (endpoint.Contains("$submit")) return "PriorAuth.Submit";
        if (endpoint.Contains("$inquire")) return "PriorAuth.Inquire";
        if (endpoint.Contains("$status")) return "ClaimStatus.Inquire";
        if (endpoint.Contains("$process-x12")) return "X12Response.Process";
        if (endpoint.Contains("x12-to-fhir")) return "Convert.X12ToFhir";
        if (endpoint.Contains("fhir-to-x12")) return "Convert.FhirToX12";
        if (endpoint.Contains("eligibility")) return "Eligibility.Convert";
        if (endpoint.Contains("claim-status")) return "ClaimStatus.Convert";
        if (endpoint.Contains("attachment")) return "Attachment.Convert";
        if (endpoint.Contains("admin")) return $"Admin.{method}";
        return $"API.{method}";
    }

    private static string InferTransactionType(string endpoint)
    {
        if (endpoint.Contains("Claim/$submit") || endpoint.Contains("Claim/$inquire") || endpoint.Contains("278"))
            return "278";
        if (endpoint.Contains("eligibility") || endpoint.Contains("270") || endpoint.Contains("271")
            || endpoint.Contains("CoverageEligibility"))
            return "270/271";
        if (endpoint.Contains("claim-status") || endpoint.Contains("276") || endpoint.Contains("277")
            || endpoint.Contains("Claim/$status") || endpoint.Contains("ClaimResponse"))
            return "276/277";
        if (endpoint.Contains("attachment") || endpoint.Contains("275") || endpoint.Contains("DocumentReference"))
            return "275";
        if (endpoint.Contains("837")) return "837";
        if (endpoint.Contains("835")) return "835";
        return "unknown";
    }

    private static Billing.ConversionDirection InferDirection(string endpoint)
    {
        if (endpoint.Contains("x12-to-fhir") || endpoint.Contains("$process-x12"))
            return Billing.ConversionDirection.X12ToFhir;
        return Billing.ConversionDirection.FhirToX12;
    }

    private static PhiAccessLevel DeterminePhiAccess(string endpoint)
    {
        if (endpoint.Contains("admin") || endpoint.Contains("health") || endpoint.Contains("templates")
            || endpoint.Contains("metadata") || endpoint.Contains("usage"))
            return PhiAccessLevel.None;
        return PhiAccessLevel.Masked;
    }
}

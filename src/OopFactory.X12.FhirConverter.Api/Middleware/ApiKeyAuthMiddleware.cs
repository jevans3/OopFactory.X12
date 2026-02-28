using OopFactory.X12.FhirConverter.Api.MultiTenancy;

namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Validates the X-Api-Key header against the tenant registry.
/// Skips authentication for health check, swagger, and metadata endpoints.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private static readonly HashSet<string> BypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/health",
        "/api/fhir/metadata",
        "/swagger",
        "/health"
    };

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip auth for health, swagger, and metadata endpoints
        if (ShouldBypass(path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader)
            || string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            _logger.LogWarning("Unauthenticated request to {Path} from {IP}",
                path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                resourceType = "OperationOutcome",
                issue = new[]
                {
                    new
                    {
                        severity = "error",
                        code = "security",
                        diagnostics = "Missing X-Api-Key header. Provide a valid API key to access this endpoint."
                    }
                }
            });
            return;
        }

        var tenant = await tenantService.ValidateApiKeyAsync(apiKeyHeader!);
        if (tenant == null)
        {
            _logger.LogWarning("Invalid API key used for {Path} from {IP}",
                path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                resourceType = "OperationOutcome",
                issue = new[]
                {
                    new
                    {
                        severity = "error",
                        code = "security",
                        diagnostics = "Invalid or expired API key."
                    }
                }
            });
            return;
        }

        if (!tenant.IsActive)
        {
            _logger.LogWarning("Suspended tenant {TenantId} attempted access to {Path}",
                tenant.TenantId, path);

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                resourceType = "OperationOutcome",
                issue = new[]
                {
                    new
                    {
                        severity = "error",
                        code = "forbidden",
                        diagnostics = "Your account has been suspended. Contact support."
                    }
                }
            });
            return;
        }

        // Store the validated tenant config on HttpContext for TenantContextMiddleware
        context.Items["TenantConfig"] = tenant;
        await _next(context);
    }

    private static bool ShouldBypass(string path)
    {
        foreach (var bypassPath in BypassPaths)
        {
            if (path.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

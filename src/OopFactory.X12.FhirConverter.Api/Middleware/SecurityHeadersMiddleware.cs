namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Adds security headers to all responses for HIPAA compliance and defense-in-depth.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS: enforce HTTPS for 1 year
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Disable browser XSS filter (API only, prefer CSP)
        context.Response.Headers["X-XSS-Protection"] = "0";

        // Content Security Policy (API-only, restrict everything)
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";

        // Don't send referrer information
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        // Permissions policy (disable unnecessary browser features)
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}

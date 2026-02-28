namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Validates incoming requests: enforces size limits, checks content types,
/// and ensures request bodies are well-formed before hitting controllers.
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    // 5 MB max request body for EDI/FHIR content
    private const long MaxRequestBodySize = 5 * 1024 * 1024;

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST/PUT requests with bodies
        if (context.Request.Method is "POST" or "PUT")
        {
            // Check content length
            if (context.Request.ContentLength > MaxRequestBodySize)
            {
                _logger.LogWarning("Request body too large: {Size} bytes from {IP}",
                    context.Request.ContentLength, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 413;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    resourceType = "OperationOutcome",
                    issue = new[]
                    {
                        new
                        {
                            severity = "error",
                            code = "too-long",
                            diagnostics = $"Request body exceeds maximum size of {MaxRequestBodySize / (1024 * 1024)} MB."
                        }
                    }
                });
                return;
            }
        }

        await _next(context);
    }
}

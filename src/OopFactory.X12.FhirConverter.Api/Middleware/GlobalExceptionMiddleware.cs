using System.Net;
using System.Text.Json;

namespace OopFactory.X12.FhirConverter.Api.Middleware;

/// <summary>
/// Global exception handler that returns structured FHIR OperationOutcome
/// error responses. Prevents stack traces from leaking to clients.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly bool _includeDetails;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _includeDetails = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in request to {Path}", context.Request.Path);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest,
                "invalid", "Invalid JSON format in request body.", ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for {Path}", context.Request.Path);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest,
                "invalid", ex.Message, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found for {Path}", context.Request.Path);
            await WriteErrorResponse(context, HttpStatusCode.NotFound,
                "not-found", ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request cancelled for {Path}", context.Request.Path);
            context.Response.StatusCode = 499; // Client closed request
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Request timeout for {Path}", context.Request.Path);
            await WriteErrorResponse(context, HttpStatusCode.GatewayTimeout,
                "timeout", "The request timed out. Please try again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "exception", "An internal error occurred. Please try again later.", ex);
        }
    }

    private async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode,
        string code, string message, Exception ex)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/fhir+json";

        var diagnostics = _includeDetails ? $"{message} Detail: {ex.Message}" : message;

        await context.Response.WriteAsJsonAsync(new
        {
            resourceType = "OperationOutcome",
            issue = new[]
            {
                new
                {
                    severity = "error",
                    code,
                    diagnostics
                }
            }
        });
    }
}

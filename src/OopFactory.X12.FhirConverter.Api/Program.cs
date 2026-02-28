using OopFactory.X12.FhirConverter.Api.Billing;
using OopFactory.X12.FhirConverter.Api.Compliance;
using OopFactory.X12.FhirConverter.Api.Configuration;
using OopFactory.X12.FhirConverter.Api.Middleware;
using OopFactory.X12.FhirConverter.Api.MultiTenancy;
using OopFactory.X12.FhirConverter.Api.Services;
using OopFactory.X12.FhirConverter.Engine;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ──────────────────────────────────────────────────────────
builder.Services.Configure<FhirConverterOptions>(
    builder.Configuration.GetSection("FhirConverter"));

var fhirOptions = builder.Configuration.GetSection("FhirConverter").Get<FhirConverterOptions>()
    ?? new FhirConverterOptions();

// ─── Core Conversion Services ───────────────────────────────────────────────
builder.Services.AddSingleton<LiquidTemplateEngine>(sp =>
{
    var templatePath = Path.Combine(AppContext.BaseDirectory, fhirOptions.TemplatePath);
    if (!Directory.Exists(templatePath))
        templatePath = Path.Combine(Directory.GetCurrentDirectory(), fhirOptions.TemplatePath);
    return new LiquidTemplateEngine(templatePath);
});

builder.Services.AddSingleton<X12ToFhirConverter>();
builder.Services.AddSingleton<FhirToX12Converter>();

// ─── Multi-Tenancy ──────────────────────────────────────────────────────────
builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<ITenantService, TenantService>();

// ─── Billing + Audit ────────────────────────────────────────────────────────
builder.Services.AddSingleton<IBillingMeterService, BillingMeterService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// ─── Orchestrator (scoped — picks up scoped TenantContext) ──────────────────
builder.Services.AddScoped<ITranslationOrchestrator, TranslationOrchestrator>();

// ─── API Framework ──────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Da Vinci X12-FHIR Translation API",
        Version = "v1",
        Description = "Multi-tenant SaaS API for bidirectional X12 EDI <-> FHIR R4 translation. " +
                      "Supports CMS 0057-F compliance including Prior Authorization (278), " +
                      "Eligibility (270/271), Claim Status (276/277), Attachments (275), " +
                      "Claims (837), and Remittance (835). " +
                      "Authenticate with X-Api-Key header.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Da Vinci Burden Reduction Coordinator",
            Url = new Uri("https://github.com/jevans3/OopFactory.X12")
        }
    });

    // Add API key authentication to Swagger UI
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "API key for tenant authentication"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

// ─── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("TenantCors", policy =>
    {
        policy.AllowAnyOrigin() // In production, restrict to registered tenant domains
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders(
                  "X-Correlation-Id",
                  "X-RateLimit-Limit",
                  "X-RateLimit-Remaining",
                  "Retry-After");
    });
});

var app = builder.Build();

// ─── Middleware Pipeline (order matters!) ────────────────────────────────────

// 1. Global exception handler (outermost — catches everything)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Security headers (HSTS, XSS protection, etc.)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3. CORS
app.UseCors("TenantCors");

// 4. Swagger (before auth so it's publicly accessible)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Da Vinci X12-FHIR Translation API v1");
    c.RoutePrefix = string.Empty;
});

// 5. Health checks (before auth)
app.UseHealthChecks("/api/health");

// 6. Request validation (size limits, content type checks)
app.UseMiddleware<RequestValidationMiddleware>();

// 7. API Key authentication
app.UseMiddleware<ApiKeyAuthMiddleware>();

// 8. Tenant context population
app.UseMiddleware<TenantContextMiddleware>();

// 9. Per-tenant rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// 10. Audit logging + billing metering
app.UseMiddleware<AuditLoggingMiddleware>();

// 11. Controllers
app.MapControllers();

app.Run();

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program { }

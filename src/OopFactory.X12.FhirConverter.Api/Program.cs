using OopFactory.X12.FhirConverter.Api.Configuration;
using OopFactory.X12.FhirConverter.Api.Services;
using OopFactory.X12.FhirConverter.Engine;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<FhirConverterOptions>(
    builder.Configuration.GetSection("FhirConverter"));

var fhirOptions = builder.Configuration.GetSection("FhirConverter").Get<FhirConverterOptions>()
    ?? new FhirConverterOptions();

// Register services
builder.Services.AddSingleton<LiquidTemplateEngine>(sp =>
{
    var templatePath = Path.Combine(AppContext.BaseDirectory, fhirOptions.TemplatePath);
    if (!Directory.Exists(templatePath))
        templatePath = Path.Combine(Directory.GetCurrentDirectory(), fhirOptions.TemplatePath);
    return new LiquidTemplateEngine(templatePath);
});

builder.Services.AddSingleton<X12ToFhirConverter>();
builder.Services.AddSingleton<FhirToX12Converter>();
builder.Services.AddScoped<ITranslationOrchestrator, TranslationOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Da Vinci X12-FHIR Translation API",
        Version = "v1",
        Description = "Containerized API for bidirectional X12 EDI <-> FHIR R4 translation. " +
                      "Supports CMS 0057-F compliance including Prior Authorization (278), " +
                      "Eligibility (270/271), Claim Status (276/277), Attachments (275), " +
                      "Claims (837), and Remittance (835).",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Da Vinci Burden Reduction Coordinator",
            Url = new Uri("https://github.com/jevans3/OopFactory.X12")
        }
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Da Vinci X12-FHIR Translation API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root
});

app.UseHealthChecks("/api/health");
app.MapControllers();

app.Run();

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program { }

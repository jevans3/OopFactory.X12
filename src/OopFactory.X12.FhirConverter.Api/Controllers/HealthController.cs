using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Engine;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly LiquidTemplateEngine _templateEngine;

    public HealthController(LiquidTemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
    }

    /// <summary>
    /// List available conversion templates.
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        var templates = _templateEngine.GetAvailableTemplates();
        return Ok(new
        {
            count = templates.Count,
            templates
        });
    }

    /// <summary>
    /// FHIR CapabilityStatement (metadata endpoint).
    /// </summary>
    [HttpGet("fhir/metadata")]
    [Produces("application/fhir+json")]
    public IActionResult GetMetadata()
    {
        return Ok(new
        {
            resourceType = "CapabilityStatement",
            status = "active",
            kind = "instance",
            fhirVersion = "4.0.1",
            format = new[] { "application/fhir+json" },
            description = "Da Vinci Burden Reduction Coordinator - X12/FHIR Translation API. " +
                          "Supports CMS 0057-F compliant bidirectional translation between " +
                          "X12 EDI (278, 275, 270/271, 276/277, 837, 835) and FHIR R4.",
            implementation = new
            {
                description = "OopFactory X12-FHIR Converter API",
                url = Request.Scheme + "://" + Request.Host
            },
            rest = new[]
            {
                new
                {
                    mode = "server",
                    resource = new object[]
                    {
                        new
                        {
                            type = "Claim",
                            operation = new[]
                            {
                                new { name = "submit", definition = "http://hl7.org/fhir/us/davinci-pas/OperationDefinition/Claim-submit" },
                                new { name = "inquire", definition = "http://hl7.org/fhir/us/davinci-pas/OperationDefinition/Claim-inquire" },
                                new { name = "status", definition = "http://hl7.org/fhir/OperationDefinition/Claim-status" }
                            }
                        },
                        new
                        {
                            type = "CoverageEligibilityRequest",
                            interaction = new[] { new { code = "create" } }
                        },
                        new
                        {
                            type = "DocumentReference",
                            interaction = new[] { new { code = "create" } }
                        }
                    }
                }
            }
        });
    }
}

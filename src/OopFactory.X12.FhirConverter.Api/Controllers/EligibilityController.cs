using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Services;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Eligibility verification endpoints (X12 270/271).
/// </summary>
[ApiController]
[Route("api/fhir")]
public class EligibilityController : ControllerBase
{
    private readonly ITranslationOrchestrator _orchestrator;

    public EligibilityController(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Submit a FHIR CoverageEligibilityRequest.
    /// Converts to X12 270 for payer submission.
    /// </summary>
    [HttpPost("CoverageEligibilityRequest")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> SubmitEligibilityRequest()
    {
        using var reader = new StreamReader(Request.Body);
        var fhirJson = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirJson))
            return BadRequest(new { error = "Request body required" });

        var x12Result = _orchestrator.ConvertFhirToX12(fhirJson, "270", "Request");
        if (!x12Result.IsSuccess)
            return BadRequest(new { error = x12Result.ErrorMessage });

        return Ok(new
        {
            resourceType = "OperationOutcome",
            issue = new[]
            {
                new
                {
                    severity = "information",
                    code = "informational",
                    diagnostics = "Eligibility request converted to X12 270 successfully."
                }
            },
            x12Output = x12Result.Output
        });
    }

    /// <summary>
    /// Process an X12 271 eligibility response and return FHIR CoverageEligibilityResponse.
    /// </summary>
    [HttpPost("CoverageEligibilityResponse/$process-x12")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> ProcessEligibilityResponse()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();

        var result = _orchestrator.ConvertX12ToFhir(x12Edi, "271");
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Content(result.Output, "application/fhir+json");
    }
}

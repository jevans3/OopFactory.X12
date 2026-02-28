using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Services;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Claim status inquiry endpoints (X12 276/277).
/// </summary>
[ApiController]
[Route("api/fhir")]
public class ClaimStatusController : ControllerBase
{
    private readonly ITranslationOrchestrator _orchestrator;

    public ClaimStatusController(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Submit a claim status inquiry.
    /// Converts FHIR Claim to X12 276.
    /// </summary>
    [HttpPost("Claim/$status")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> SubmitClaimStatus()
    {
        using var reader = new StreamReader(Request.Body);
        var fhirJson = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirJson))
            return BadRequest(new { error = "Request body required" });

        var x12Result = _orchestrator.ConvertFhirToX12(fhirJson, "276", "Request");
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
                    diagnostics = "Claim status inquiry converted to X12 276 successfully."
                }
            },
            x12Output = x12Result.Output
        });
    }

    /// <summary>
    /// Process an X12 277 claim status response and return FHIR ClaimResponse.
    /// </summary>
    [HttpPost("ClaimResponse/$process-x12")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> ProcessClaimStatusResponse()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();

        var result = _orchestrator.ConvertX12ToFhir(x12Edi, "277");
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Content(result.Output, "application/fhir+json");
    }
}

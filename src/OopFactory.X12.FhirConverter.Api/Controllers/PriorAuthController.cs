using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Services;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Da Vinci PAS (Prior Authorization Support) operations.
/// Implements the $submit and $inquire operations for CMS 0057-F compliance.
/// </summary>
[ApiController]
[Route("api/fhir")]
public class PriorAuthController : ControllerBase
{
    private readonly ITranslationOrchestrator _orchestrator;

    public PriorAuthController(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Da Vinci PAS $submit operation.
    /// Accepts a FHIR Bundle containing a Claim (preauthorization) and supporting resources.
    /// Converts to X12 278 Request, and returns a FHIR ClaimResponse Bundle.
    ///
    /// POST /api/fhir/Claim/$submit
    /// Content-Type: application/fhir+json
    /// </summary>
    [HttpPost("Claim/$submit")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> SubmitPriorAuth()
    {
        using var reader = new StreamReader(Request.Body);
        var fhirBundle = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirBundle))
            return BadRequest(CreateOperationOutcome("error", "invalid",
                "Request body must contain a FHIR Bundle with a Claim resource"));

        // Step 1: Convert FHIR Bundle to X12 278 Request
        var x12Result = _orchestrator.ConvertFhirToX12(fhirBundle, "278", "Request");
        if (!x12Result.IsSuccess)
            return BadRequest(CreateOperationOutcome("error", "processing",
                $"Failed to convert to X12 278: {x12Result.ErrorMessage}"));

        // Step 2: In a production deployment, the X12 278 would be forwarded to
        // the payer's EDI gateway here. For now, we return the X12 as a
        // demonstration and include it in the response.

        // Step 3: Return confirmation response
        // In production, this would convert the payer's X12 278 response back to FHIR
        var response = new
        {
            resourceType = "Bundle",
            type = "collection",
            entry = new[]
            {
                new
                {
                    resource = new
                    {
                        resourceType = "OperationOutcome",
                        issue = new[]
                        {
                            new
                            {
                                severity = "information",
                                code = "informational",
                                diagnostics = "Prior authorization request converted to X12 278 successfully. " +
                                             "In production, this would be forwarded to the payer EDI gateway.",
                                details = new
                                {
                                    text = "X12 278 generated. Awaiting payer response."
                                }
                            }
                        }
                    }
                }
            },
            x12Output = x12Result.Output
        };

        return Ok(response);
    }

    /// <summary>
    /// Da Vinci PAS $inquire operation.
    /// Checks the status of a previously submitted prior authorization.
    /// </summary>
    [HttpPost("Claim/$inquire")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> InquirePriorAuth()
    {
        using var reader = new StreamReader(Request.Body);
        var fhirBundle = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirBundle))
            return BadRequest(CreateOperationOutcome("error", "invalid",
                "Request body must contain a FHIR Bundle with inquiry parameters"));

        // Convert the inquiry to X12 278 inquiry
        var x12Result = _orchestrator.ConvertFhirToX12(fhirBundle, "278", "Request");
        if (!x12Result.IsSuccess)
            return BadRequest(CreateOperationOutcome("error", "processing",
                $"Failed to convert inquiry: {x12Result.ErrorMessage}"));

        return Ok(new
        {
            resourceType = "OperationOutcome",
            issue = new[]
            {
                new
                {
                    severity = "information",
                    code = "informational",
                    diagnostics = "Prior authorization inquiry converted to X12 278 successfully."
                }
            },
            x12Output = x12Result.Output
        });
    }

    /// <summary>
    /// Convert an X12 278 Response to a FHIR ClaimResponse Bundle.
    /// Used when receiving a payer's X12 278 response.
    /// </summary>
    [HttpPost("Claim/$process-x12-response")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> ProcessX12Response()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Response = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(x12Response))
            return BadRequest(CreateOperationOutcome("error", "invalid",
                "Request body must contain X12 278 Response EDI data"));

        var fhirResult = _orchestrator.ConvertX12ToFhir(x12Response, "278");
        if (!fhirResult.IsSuccess)
            return BadRequest(CreateOperationOutcome("error", "processing",
                $"Failed to convert X12 278 response: {fhirResult.ErrorMessage}"));

        return Content(fhirResult.Output, "application/fhir+json");
    }

    private static object CreateOperationOutcome(string severity, string code, string message)
    {
        return new
        {
            resourceType = "OperationOutcome",
            issue = new[]
            {
                new { severity, code, diagnostics = message }
            }
        };
    }
}

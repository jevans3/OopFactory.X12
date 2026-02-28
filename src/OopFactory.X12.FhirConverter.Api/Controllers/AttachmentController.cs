using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Services;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

/// <summary>
/// Attachment/Additional Information endpoints (X12 275).
/// Implements CDex attachment exchange for CMS 0057-F.
/// </summary>
[ApiController]
[Route("api/fhir")]
public class AttachmentController : ControllerBase
{
    private readonly ITranslationOrchestrator _orchestrator;

    public AttachmentController(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Submit a FHIR DocumentReference with attachment.
    /// Converts to X12 275 for payer submission.
    /// </summary>
    [HttpPost("DocumentReference")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> SubmitAttachment()
    {
        using var reader = new StreamReader(Request.Body);
        var fhirJson = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirJson))
            return BadRequest(new { error = "Request body required" });

        var x12Result = _orchestrator.ConvertFhirToX12(fhirJson, "275", "Attachment");
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
                    diagnostics = "Attachment converted to X12 275 successfully."
                }
            },
            x12Output = x12Result.Output
        });
    }

    /// <summary>
    /// Process an X12 275 and return FHIR DocumentReference.
    /// </summary>
    [HttpPost("DocumentReference/$process-x12")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> ProcessX12Attachment()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();

        var result = _orchestrator.ConvertX12ToFhir(x12Edi, "275");
        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Content(result.Output, "application/fhir+json");
    }
}

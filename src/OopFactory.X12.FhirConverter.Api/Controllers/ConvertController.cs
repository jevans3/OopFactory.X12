using Microsoft.AspNetCore.Mvc;
using OopFactory.X12.FhirConverter.Api.Models;
using OopFactory.X12.FhirConverter.Api.Services;

namespace OopFactory.X12.FhirConverter.Api.Controllers;

[ApiController]
[Route("api/convert")]
public class ConvertController : ControllerBase
{
    private readonly ITranslationOrchestrator _orchestrator;

    public ConvertController(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Convert X12 EDI to FHIR R4 JSON.
    /// Auto-detects transaction type from ST segment, or specify via query parameter.
    /// </summary>
    [HttpPost("x12-to-fhir")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json", "application/json")]
    public async Task<IActionResult> X12ToFhir(
        [FromQuery] string? transactionType = null)
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(x12Edi))
            return BadRequest(new { error = "Request body must contain X12 EDI data" });

        var result = _orchestrator.ConvertX12ToFhir(x12Edi, transactionType);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Content(result.Output, "application/fhir+json");
    }

    /// <summary>
    /// Convert FHIR R4 JSON to X12 EDI.
    /// Transaction type must be specified.
    /// </summary>
    [HttpPost("fhir-to-x12")]
    [Consumes("application/fhir+json", "application/json")]
    [Produces("text/plain", "application/edi-x12")]
    public async Task<IActionResult> FhirToX12(
        [FromQuery] string transactionType)
    {
        if (string.IsNullOrWhiteSpace(transactionType))
            return BadRequest(new { error = "transactionType query parameter is required" });

        using var reader = new StreamReader(Request.Body);
        var fhirJson = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(fhirJson))
            return BadRequest(new { error = "Request body must contain FHIR JSON" });

        var result = _orchestrator.ConvertFhirToX12(fhirJson, transactionType);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Content(result.Output, "application/edi-x12");
    }

    /// <summary>
    /// Convert X12 Eligibility (270/271) to FHIR.
    /// </summary>
    [HttpPost("eligibility/x12-to-fhir")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> EligibilityX12ToFhir()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();
        var result = _orchestrator.ConvertX12ToFhir(x12Edi);
        if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
        return Content(result.Output, "application/fhir+json");
    }

    /// <summary>
    /// Convert X12 Claim Status (276/277) to FHIR.
    /// </summary>
    [HttpPost("claim-status/x12-to-fhir")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> ClaimStatusX12ToFhir()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();
        var result = _orchestrator.ConvertX12ToFhir(x12Edi);
        if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
        return Content(result.Output, "application/fhir+json");
    }

    /// <summary>
    /// Convert X12 Attachment (275) to FHIR DocumentReference.
    /// </summary>
    [HttpPost("attachment/x12-to-fhir")]
    [Consumes("text/plain", "application/edi-x12")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> AttachmentX12ToFhir()
    {
        using var reader = new StreamReader(Request.Body);
        var x12Edi = await reader.ReadToEndAsync();
        var result = _orchestrator.ConvertX12ToFhir(x12Edi, "275");
        if (!result.IsSuccess) return BadRequest(new { error = result.ErrorMessage });
        return Content(result.Output, "application/fhir+json");
    }
}

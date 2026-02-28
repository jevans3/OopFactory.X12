using OopFactory.X12.FhirConverter.Engine;

namespace OopFactory.X12.FhirConverter.Api.Services;

public class TranslationOrchestrator : ITranslationOrchestrator
{
    private readonly X12ToFhirConverter _x12ToFhir;
    private readonly FhirToX12Converter _fhirToX12;
    private readonly ILogger<TranslationOrchestrator> _logger;

    public TranslationOrchestrator(
        X12ToFhirConverter x12ToFhir,
        FhirToX12Converter fhirToX12,
        ILogger<TranslationOrchestrator> logger)
    {
        _x12ToFhir = x12ToFhir;
        _fhirToX12 = fhirToX12;
        _logger = logger;
    }

    public ConversionResult ConvertX12ToFhir(string x12Edi, string? transactionType = null)
    {
        _logger.LogInformation("Converting X12 to FHIR. Transaction type: {TransactionType}",
            transactionType ?? "auto-detect");

        try
        {
            var result = _x12ToFhir.Convert(x12Edi, transactionType);

            if (result.IsSuccess)
                _logger.LogInformation("X12 to FHIR conversion successful. Type: {Type}", result.TransactionType);
            else
                _logger.LogWarning("X12 to FHIR conversion failed: {Error}", result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "X12 to FHIR conversion error");
            return ConversionResult.Error($"Conversion failed: {ex.Message}");
        }
    }

    public ConversionResult ConvertFhirToX12(string fhirJson, string transactionType, string? templateName = null)
    {
        _logger.LogInformation("Converting FHIR to X12. Transaction type: {TransactionType}", transactionType);

        try
        {
            var result = _fhirToX12.Convert(fhirJson, transactionType, templateName);

            if (result.IsSuccess)
                _logger.LogInformation("FHIR to X12 conversion successful. Type: {Type}", result.TransactionType);
            else
                _logger.LogWarning("FHIR to X12 conversion failed: {Error}", result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FHIR to X12 conversion error");
            return ConversionResult.Error($"Conversion failed: {ex.Message}");
        }
    }
}

using OopFactory.X12.FhirConverter.Engine;

namespace OopFactory.X12.FhirConverter.Api.Services;

public interface ITranslationOrchestrator
{
    ConversionResult ConvertX12ToFhir(string x12Edi, string? transactionType = null);
    ConversionResult ConvertFhirToX12(string fhirJson, string transactionType, string? templateName = null);
}

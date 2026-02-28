namespace OopFactory.X12.FhirConverter.Api.Models;

public class ConversionRequest
{
    public string Input { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public ConversionDirection Direction { get; set; }
}

public class ConversionResponse
{
    public bool Success { get; set; }
    public string Output { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public enum ConversionDirection
{
    X12ToFhir,
    FhirToX12
}

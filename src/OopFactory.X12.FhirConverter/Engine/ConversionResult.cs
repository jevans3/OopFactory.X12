namespace OopFactory.X12.FhirConverter.Engine;

public class ConversionResult
{
    public bool IsSuccess { get; init; }
    public string Output { get; init; } = "";
    public string TransactionType { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();

    public static ConversionResult Success(string output, string transactionType)
    {
        return new ConversionResult
        {
            IsSuccess = true,
            Output = output,
            TransactionType = transactionType
        };
    }

    public static ConversionResult Error(string message)
    {
        return new ConversionResult
        {
            IsSuccess = false,
            ErrorMessage = message
        };
    }
}

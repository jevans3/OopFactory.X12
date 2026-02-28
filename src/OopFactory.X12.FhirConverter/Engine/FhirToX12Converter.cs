using System.Text.Json;
using DotLiquid;
using OopFactory.X12.Parsing;

namespace OopFactory.X12.FhirConverter.Engine;

public class FhirToX12Converter
{
    private readonly LiquidTemplateEngine _templateEngine;
    private readonly X12Parser _parser;

    public FhirToX12Converter(LiquidTemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
        _parser = new X12Parser(false);
    }

    /// <summary>
    /// Convert FHIR JSON to X12 EDI.
    /// </summary>
    public ConversionResult Convert(string fhirJson, string transactionType, string? templateName = null)
    {
        try
        {
            var fhirData = ParseFhirToHash(fhirJson);
            var resolvedTemplateName = templateName ?? DetermineTemplateName(fhirJson, transactionType);
            var x12Xml = _templateEngine.RenderFhirToX12(transactionType, resolvedTemplateName, fhirData);

            // The template outputs X12-as-XML; transform to raw X12 EDI
            string x12Edi;
            if (x12Xml.TrimStart().StartsWith("<"))
            {
                x12Edi = _parser.TransformToX12(x12Xml);
            }
            else
            {
                // Template directly produced X12 EDI
                x12Edi = x12Xml;
            }

            return ConversionResult.Success(x12Edi, transactionType);
        }
        catch (Exception ex)
        {
            return ConversionResult.Error($"FHIR to X12 conversion failed: {ex.Message}");
        }
    }

    private string DetermineTemplateName(string fhirJson, string transactionType)
    {
        return transactionType switch
        {
            "278" => DeterminePasDirection(fhirJson),
            "275" => "Attachment",
            "270" => "Request",
            "271" => "Response",
            "276" => "Request",
            "277" => "Response",
            "837" => "Claim",
            "835" => "Remittance",
            _ => "Default"
        };
    }

    private string DeterminePasDirection(string fhirJson)
    {
        // Check if this is a ClaimResponse (PA response) or Claim (PA request)
        if (fhirJson.Contains("\"ClaimResponse\""))
            return "Response";
        return "Request";
    }

    private Hash ParseFhirToHash(string fhirJson)
    {
        var jsonDoc = JsonDocument.Parse(fhirJson);
        var data = JsonElementToDict(jsonDoc.RootElement);
        return Hash.FromDictionary(data);
    }

    private Dictionary<string, object> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonValue(property.Value);
            }
        }

        return dict;
    }

    private object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonElementToDict(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => "",
            _ => element.ToString()
        };
    }
}

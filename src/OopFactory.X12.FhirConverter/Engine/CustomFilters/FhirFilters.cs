using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OopFactory.X12.FhirConverter.Engine.CustomFilters;

public static class FhirFilters
{
    /// <summary>
    /// Create a FHIR Reference JSON fragment.
    /// </summary>
    public static string FhirReference(string id, string resourceType)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(resourceType)) return "null";
        return $"\"reference\": \"{resourceType}/{id}\"";
    }

    /// <summary>
    /// Generate a deterministic UUID v5 from a resource type and identifier.
    /// </summary>
    public static string GenerateUuid(string resourceType, string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return Guid.NewGuid().ToString();
        var input = $"{resourceType}:{identifier}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50); // Version 5
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // Variant 1
        return new Guid(guidBytes).ToString();
    }

    /// <summary>
    /// Map X12 gender code to FHIR administrative gender.
    /// </summary>
    public static string X12Gender(string code)
    {
        return code?.ToUpperInvariant() switch
        {
            "M" => "male",
            "F" => "female",
            "U" => "unknown",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Map FHIR administrative gender to X12 gender code.
    /// </summary>
    public static string FhirGenderToX12(string gender)
    {
        return gender?.ToLowerInvariant() switch
        {
            "male" => "M",
            "female" => "F",
            "unknown" => "U",
            _ => "U"
        };
    }

    /// <summary>
    /// Create a FHIR CodeableConcept JSON fragment.
    /// </summary>
    public static string CodeableConcept(string system, string code, string display)
    {
        if (string.IsNullOrEmpty(code)) return "null";
        var sb = new StringBuilder();
        sb.Append("{\"coding\": [{");
        sb.Append($"\"system\": \"{EscapeJson(system)}\",");
        sb.Append($"\"code\": \"{EscapeJson(code)}\"");
        if (!string.IsNullOrEmpty(display))
            sb.Append($",\"display\": \"{EscapeJson(display)}\"");
        sb.Append("}]}");
        return sb.ToString();
    }

    /// <summary>
    /// Create a FHIR Identifier JSON fragment.
    /// </summary>
    public static string FhirIdentifier(string system, string value)
    {
        if (string.IsNullOrEmpty(value)) return "null";
        return $"{{\"system\": \"{EscapeJson(system)}\", \"value\": \"{EscapeJson(value)}\"}}";
    }

    /// <summary>
    /// Map X12 relationship code to FHIR Beneficiary relationship.
    /// </summary>
    public static string X12RelationshipToFhir(string code)
    {
        return code switch
        {
            "18" => "self",
            "01" => "spouse",
            "19" => "child",
            "20" => "employee",
            "21" => "unknown",
            "39" => "parent",
            "40" => "grandparent",
            "53" => "other",
            "G8" => "other",
            _ => "other"
        };
    }

    /// <summary>
    /// Escape a string for use in JSON.
    /// </summary>
    public static string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Wrap a value in a FHIR extension.
    /// </summary>
    public static string FhirExtension(string url, string valueType, string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return $"{{\"url\": \"{EscapeJson(url)}\", \"{valueType}\": \"{EscapeJson(value)}\"}}";
    }
}

using System.Reflection;
using System.Text.Json;

namespace OopFactory.X12.FhirConverter.Engine.CustomFilters;

public static class CodeMappingFilters
{
    private static readonly Lazy<Dictionary<string, Dictionary<string, CodeMapping>>> _mappings
        = new(LoadAllMappings);

    private static Dictionary<string, Dictionary<string, CodeMapping>> LoadAllMappings()
    {
        var result = new Dictionary<string, Dictionary<string, CodeMapping>>(StringComparer.OrdinalIgnoreCase);
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "OopFactory.X12.FhirConverter.CodeMappings.";

        foreach (var resourceName in assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix) && n.EndsWith(".json")))
        {
            var mapName = resourceName[prefix.Length..^5]; // Remove prefix and .json
            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var mappings = JsonSerializer.Deserialize<Dictionary<string, CodeMapping>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (mappings != null)
                result[mapName] = mappings;
        }

        return result;
    }

    /// <summary>
    /// Map an X12 code to its FHIR equivalent using a named mapping table.
    /// </summary>
    public static string X12CodeToFhir(string x12Code, string mappingName)
    {
        if (string.IsNullOrEmpty(x12Code) || string.IsNullOrEmpty(mappingName)) return x12Code ?? "";

        if (_mappings.Value.TryGetValue(mappingName, out var map) &&
            map.TryGetValue(x12Code, out var mapping))
        {
            return mapping.FhirCode ?? x12Code;
        }
        return x12Code;
    }

    /// <summary>
    /// Get the FHIR system URI for an X12 code mapping.
    /// </summary>
    public static string X12CodeSystem(string x12Code, string mappingName)
    {
        if (string.IsNullOrEmpty(x12Code) || string.IsNullOrEmpty(mappingName)) return "";

        if (_mappings.Value.TryGetValue(mappingName, out var map) &&
            map.TryGetValue(x12Code, out var mapping))
        {
            return mapping.FhirSystem ?? "";
        }
        return "";
    }

    /// <summary>
    /// Get the display name for an X12 code.
    /// </summary>
    public static string X12CodeDisplay(string x12Code, string mappingName)
    {
        if (string.IsNullOrEmpty(x12Code) || string.IsNullOrEmpty(mappingName)) return "";

        if (_mappings.Value.TryGetValue(mappingName, out var map) &&
            map.TryGetValue(x12Code, out var mapping))
        {
            return mapping.Display ?? "";
        }
        return "";
    }

    /// <summary>
    /// Reverse map a FHIR code to X12 code.
    /// </summary>
    public static string FhirCodeToX12(string fhirCode, string mappingName)
    {
        if (string.IsNullOrEmpty(fhirCode) || string.IsNullOrEmpty(mappingName)) return fhirCode ?? "";

        if (_mappings.Value.TryGetValue(mappingName, out var map))
        {
            var entry = map.Values.FirstOrDefault(v =>
                string.Equals(v.FhirCode, fhirCode, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                var key = map.FirstOrDefault(kvp => kvp.Value == entry).Key;
                return key ?? fhirCode;
            }
        }
        return fhirCode;
    }

    /// <summary>
    /// Map X12 diagnosis code qualifier to FHIR coding system.
    /// </summary>
    public static string DiagnosisCodeSystem(string qualifier)
    {
        return qualifier switch
        {
            "ABK" => "http://hl7.org/fhir/sid/icd-10-cm",
            "ABF" => "http://hl7.org/fhir/sid/icd-10-cm",
            "BK" => "http://hl7.org/fhir/sid/icd-9-cm",
            "BF" => "http://hl7.org/fhir/sid/icd-9-cm",
            "ABJ" => "http://hl7.org/fhir/sid/icd-10-pcs",
            "ABN" => "http://hl7.org/fhir/sid/icd-10-pcs",
            _ => "http://hl7.org/fhir/sid/icd-10-cm"
        };
    }

    /// <summary>
    /// Map X12 procedure code qualifier to FHIR coding system.
    /// </summary>
    public static string ProcedureCodeSystem(string qualifier)
    {
        return qualifier switch
        {
            "HC" => "http://www.ama-assn.org/go/cpt",
            "HP" => "https://www.cms.gov/Medicare/Coding/HCPCSReleaseCodeSets",
            "IV" => "https://www.cms.gov/Medicare/Coding/HCPCSReleaseCodeSets",
            "ZZ" => "http://www.ama-assn.org/go/cpt",
            "ER" => "http://snomed.info/sct",
            _ => "http://www.ama-assn.org/go/cpt"
        };
    }
}

public class CodeMapping
{
    public string? FhirCode { get; set; }
    public string? FhirSystem { get; set; }
    public string? Display { get; set; }
}

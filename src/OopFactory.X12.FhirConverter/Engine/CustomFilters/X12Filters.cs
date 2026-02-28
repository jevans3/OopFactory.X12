using System.Xml.Linq;

namespace OopFactory.X12.FhirConverter.Engine.CustomFilters;

public static class X12Filters
{
    /// <summary>
    /// Extract a specific element value from an X12 XML segment by position.
    /// X12 XML uses elements like &lt;X1201&gt;, &lt;X1202&gt;, etc. within segment nodes.
    /// </summary>
    public static string SegmentElement(string segmentXml, int position)
    {
        if (string.IsNullOrEmpty(segmentXml)) return "";
        try
        {
            var element = XElement.Parse(segmentXml);
            var pos = position.ToString("D2");
            var child = element.Elements().FirstOrDefault(e => e.Name.LocalName.EndsWith(pos));
            return child?.Value ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Extract a sub-element from a composite X12 element.
    /// Composite elements are separated by ':' in X12.
    /// </summary>
    public static string SubElement(string compositeValue, int position)
    {
        if (string.IsNullOrEmpty(compositeValue)) return "";
        var parts = compositeValue.Split(':');
        return position > 0 && position <= parts.Length ? parts[position - 1] : "";
    }

    /// <summary>
    /// Get the X12 segment ID from an XML element.
    /// </summary>
    public static string SegmentId(string segmentXml)
    {
        if (string.IsNullOrEmpty(segmentXml)) return "";
        try
        {
            var element = XElement.Parse(segmentXml);
            return element.Name.LocalName;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Check if an X12 element equals a specific value.
    /// </summary>
    public static bool ElementEquals(string value, string expected)
    {
        return string.Equals(value?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extract entity name from NM1 segment fields.
    /// NM103 = Last/Org Name, NM104 = First, NM105 = Middle.
    /// </summary>
    public static string FormatName(string lastName, string firstName, string middleName)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(firstName)) parts.Add(firstName);
        if (!string.IsNullOrEmpty(middleName)) parts.Add(middleName);
        if (!string.IsNullOrEmpty(lastName)) parts.Add(lastName);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Parse X12 NM1 entity identifier code to determine FHIR resource type.
    /// </summary>
    public static string EntityToResourceType(string entityCode)
    {
        return entityCode switch
        {
            "IL" => "Patient",         // Insured/Subscriber
            "QC" => "Patient",         // Patient
            "85" => "Practitioner",    // Billing Provider
            "87" => "Practitioner",    // Pay-to Provider
            "DN" => "Practitioner",    // Referring Provider
            "82" => "Practitioner",    // Rendering Provider
            "77" => "Organization",    // Service Location
            "FA" => "Organization",    // Facility
            "PR" => "Organization",    // Payer
            "PE" => "Organization",    // Payee
            "1P" => "Practitioner",    // Provider
            "71" => "Practitioner",    // Attending Physician
            "72" => "Practitioner",    // Operating Physician
            "ZZ" => "Practitioner",    // Mutually Defined
            "P3" => "Practitioner",    // Primary Care Provider
            "DK" => "Practitioner",    // Ordering Provider
            "DQ" => "Practitioner",    // Supervising Physician
            _ => "Organization"
        };
    }
}

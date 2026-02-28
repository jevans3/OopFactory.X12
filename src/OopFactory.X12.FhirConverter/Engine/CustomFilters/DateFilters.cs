using System.Globalization;

namespace OopFactory.X12.FhirConverter.Engine.CustomFilters;

public static class DateFilters
{
    /// <summary>
    /// Convert X12 date format (CCYYMMDD) to FHIR date format (YYYY-MM-DD).
    /// </summary>
    public static string X12Date(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        if (input.Length == 8)
            return $"{input[..4]}-{input[4..6]}-{input[6..8]}";
        if (input.Length == 6)
            return $"{input[..4]}-{input[4..6]}";
        return input;
    }

    /// <summary>
    /// Convert X12 time format (HHMM or HHMMSS) to FHIR time format.
    /// </summary>
    public static string X12Time(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        if (input.Length >= 4)
        {
            var result = $"{input[..2]}:{input[2..4]}";
            if (input.Length >= 6)
                result += $":{input[4..6]}";
            return result;
        }
        return input;
    }

    /// <summary>
    /// Convert X12 date range (CCYYMMDD-CCYYMMDD) to FHIR Period start/end.
    /// </summary>
    public static string X12DateRangeStart(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var parts = input.Split('-');
        return parts.Length > 0 ? X12Date(parts[0]) : "";
    }

    public static string X12DateRangeEnd(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var parts = input.Split('-');
        return parts.Length > 1 ? X12Date(parts[1]) : "";
    }

    /// <summary>
    /// Convert FHIR date (YYYY-MM-DD) to X12 format (CCYYMMDD).
    /// </summary>
    public static string FhirDateToX12(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("-", "");
    }

    /// <summary>
    /// Convert FHIR dateTime to X12 date component.
    /// </summary>
    public static string FhirDateTimeToX12Date(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("yyyyMMdd");
        return FhirDateToX12(input.Length >= 10 ? input[..10] : input);
    }

    /// <summary>
    /// Generate FHIR instant from X12 date and time.
    /// </summary>
    public static string X12ToFhirInstant(string date, string time)
    {
        var fhirDate = X12Date(date);
        if (!string.IsNullOrEmpty(time))
        {
            var fhirTime = X12Time(time);
            return $"{fhirDate}T{fhirTime}:00Z";
        }
        return $"{fhirDate}T00:00:00Z";
    }
}

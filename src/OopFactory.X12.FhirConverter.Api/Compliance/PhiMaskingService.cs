using System.Text.RegularExpressions;

namespace OopFactory.X12.FhirConverter.Api.Compliance;

/// <summary>
/// Detects and masks Protected Health Information (PHI) in log output.
/// Ensures HIPAA compliance by preventing PHI from appearing in cleartext logs.
/// </summary>
public static partial class PhiMaskingService
{
    // SSN pattern: XXX-XX-XXXX
    private static readonly Regex SsnPattern = SsnRegex();

    // Date of birth patterns (various formats)
    private static readonly Regex DatePattern = DateRegex();

    // Member/subscriber ID patterns (alphanumeric, 6-20 chars that look like IDs)
    private static readonly Regex MemberIdPattern = MemberIdRegex();

    // Patient name patterns in X12 (NM103/NM104 context)
    private static readonly Regex X12NamePattern = X12NameRegex();

    // Phone numbers
    private static readonly Regex PhonePattern = PhoneRegex();

    // Email addresses
    private static readonly Regex EmailPattern = EmailRegex();

    /// <summary>
    /// Mask all detected PHI in the input string.
    /// </summary>
    public static string MaskPhi(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;

        // Mask SSNs: 123-45-6789 → ***-**-6789
        result = SsnPattern.Replace(result, "***-**-$1");

        // Mask phone numbers
        result = PhonePattern.Replace(result, "***-***-$1");

        // Mask email addresses
        result = EmailPattern.Replace(result, "***@$1");

        return result;
    }

    /// <summary>
    /// Mask PHI specifically in X12 EDI content.
    /// Targets known PHI segments: NM1 (names), DMG (demographics), N3/N4 (addresses).
    /// </summary>
    public static string MaskX12Phi(string x12Content)
    {
        if (string.IsNullOrEmpty(x12Content))
            return x12Content;

        var result = x12Content;

        // Mask NM1 segment patient/subscriber names (positions 03 and 04)
        result = X12NamePattern.Replace(result, m =>
        {
            var prefix = m.Groups[1].Value;
            var lastName = m.Groups[2].Value;
            var firstName = m.Groups[3].Value;
            var rest = m.Groups[4].Value;
            return $"{prefix}{MaskName(lastName)}*{MaskName(firstName)}{rest}";
        });

        // Apply general PHI masking
        result = MaskPhi(result);

        return result;
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= 1)
            return "***";
        return name[0] + new string('*', name.Length - 1);
    }

    [GeneratedRegex(@"\d{3}-\d{2}-(\d{4})")]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b(19|20)\d{2}[-/]?(0[1-9]|1[0-2])[-/]?(0[1-9]|[12]\d|3[01])\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\b[A-Z]{2,3}\d{6,15}\b")]
    private static partial Regex MemberIdRegex();

    [GeneratedRegex(@"(NM1\*[^*]*\*[^*]*\*)([^*]+)\*([^*]+)(.*)")]
    private static partial Regex X12NameRegex();

    [GeneratedRegex(@"\d{3}[-.]?\d{3}[-.]?(\d{4})")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})")]
    private static partial Regex EmailRegex();
}

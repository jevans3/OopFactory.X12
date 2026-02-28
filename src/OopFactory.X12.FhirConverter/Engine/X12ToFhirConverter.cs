using System.Text;
using System.Xml.Linq;
using DotLiquid;
using OopFactory.X12.Parsing;
using OopFactory.X12.Parsing.Model;

namespace OopFactory.X12.FhirConverter.Engine;

public class X12ToFhirConverter
{
    private readonly LiquidTemplateEngine _templateEngine;
    private readonly X12Parser _parser;

    public X12ToFhirConverter(LiquidTemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
        _parser = new X12Parser(false);
    }

    public X12ToFhirConverter(LiquidTemplateEngine templateEngine, X12Parser parser)
    {
        _templateEngine = templateEngine;
        _parser = parser;
    }

    /// <summary>
    /// Convert raw X12 EDI to FHIR JSON.
    /// </summary>
    public ConversionResult Convert(string x12Edi, string? transactionType = null)
    {
        var interchanges = _parser.ParseMultiple(x12Edi);
        if (interchanges.Count == 0)
            return ConversionResult.Error("No interchange found in X12 input");

        var results = new List<string>();
        foreach (var interchange in interchanges)
        {
            foreach (var fg in interchange.FunctionGroups)
            {
                foreach (var transaction in fg.Transactions)
                {
                    var detectedType = transactionType ?? transaction.IdentifierCode;
                    var templateName = DetermineTemplateName(detectedType, transaction);

                    var xml = interchange.Serialize();
                    var data = BuildTemplateData(xml, interchange, fg, transaction, detectedType);
                    var fhirJson = _templateEngine.RenderX12ToFhir(detectedType, templateName, data);
                    results.Add(fhirJson);
                }
            }
        }

        if (results.Count == 1)
            return ConversionResult.Success(results[0], transactionType ?? "unknown");

        // Multiple transactions: wrap in array
        var combined = "[" + string.Join(",", results) + "]";
        return ConversionResult.Success(combined, transactionType ?? "unknown");
    }

    /// <summary>
    /// Convert a pre-parsed Interchange to FHIR JSON.
    /// </summary>
    public ConversionResult Convert(Interchange interchange, string? transactionType = null)
    {
        var xml = interchange.Serialize();
        var results = new List<string>();

        foreach (var fg in interchange.FunctionGroups)
        {
            foreach (var transaction in fg.Transactions)
            {
                var detectedType = transactionType ?? transaction.IdentifierCode;
                var templateName = DetermineTemplateName(detectedType, transaction);
                var data = BuildTemplateData(xml, interchange, fg, transaction, detectedType);
                var fhirJson = _templateEngine.RenderX12ToFhir(detectedType, templateName, data);
                results.Add(fhirJson);
            }
        }

        if (results.Count == 1)
            return ConversionResult.Success(results[0], transactionType ?? "unknown");

        var combined = "[" + string.Join(",", results) + "]";
        return ConversionResult.Success(combined, transactionType ?? "unknown");
    }

    private string DetermineTemplateName(string transactionType, Transaction transaction)
    {
        return transactionType switch
        {
            "278" => Is278Request(transaction) ? "Request" : "Response",
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

    private bool Is278Request(Transaction transaction)
    {
        // Check BHT segment for transaction type indicator
        // BHT06 = "13" for request, "11" for response in 278
        var segments = transaction.Segments;
        foreach (var seg in segments)
        {
            if (seg.SegmentId == "BHT")
            {
                var bht06 = seg.GetElement(6);
                return bht06 != "11"; // Not a response = request
            }
        }
        return true; // Default to request
    }

    private Hash BuildTemplateData(string fullXml, Interchange interchange,
        FunctionGroup functionGroup, Transaction transaction, string transactionType)
    {
        var xmlDoc = XDocument.Parse(fullXml);

        var data = new Dictionary<string, object>
        {
            ["interchange_xml"] = fullXml,
            ["transaction_type"] = transactionType,
            ["interchange_sender_id"] = interchange.InterchangeSenderId?.Trim() ?? "",
            ["interchange_receiver_id"] = interchange.InterchangeReceiverId?.Trim() ?? "",
            ["interchange_date"] = interchange.InterchangeDate ?? "",
            ["interchange_control_number"] = interchange.InterchangeControlNumber ?? "",
            ["functional_group_code"] = functionGroup.FunctionalIdentifierCode ?? "",
            ["transaction_control_number"] = transaction.ControlNumber ?? "",
            ["transaction_identifier_code"] = transaction.IdentifierCode ?? "",
        };

        // Parse segments from the transaction into accessible structures
        var segments = new List<Dictionary<string, object>>();
        ParseContainerSegments(transaction, segments);
        data["segments"] = segments;

        // Parse hierarchical loops
        var hloops = new List<Dictionary<string, object>>();
        ParseHierarchicalLoops(transaction, hloops);
        data["hierarchical_loops"] = hloops;

        // Extract specific segment types for easier template access
        ExtractNamedSegments(segments, data);

        return Hash.FromDictionary(data);
    }

    private void ParseContainerSegments(Container container, List<Dictionary<string, object>> segments)
    {
        foreach (var segment in container.Segments)
        {
            var segData = new Dictionary<string, object>
            {
                ["id"] = segment.SegmentId,
                ["segment_string"] = segment.SegmentString
            };

            for (int i = 1; i <= 20; i++)
            {
                var value = segment.GetElement(i);
                if (value != null)
                    segData[$"e{i:D2}"] = value;
                else
                    break;
            }

            segments.Add(segData);
        }

        if (container is LoopContainer loopContainer)
        {
            foreach (var loop in loopContainer.Loops)
            {
                var loopData = new Dictionary<string, object>
                {
                    ["id"] = "LOOP",
                    ["loop_id"] = loop.Specification?.LoopId ?? ""
                };
                segments.Add(loopData);
                ParseContainerSegments(loop, segments);
            }
        }
    }

    private void ParseHierarchicalLoops(Transaction transaction, List<Dictionary<string, object>> hloops)
    {
        foreach (var hloop in transaction.HLoops)
        {
            var loopData = BuildHLoopData(hloop);
            hloops.Add(loopData);
        }
    }

    private Dictionary<string, object> BuildHLoopData(HierarchicalLoop hloop)
    {
        var data = new Dictionary<string, object>
        {
            ["id"] = hloop.Id,
            ["parent_id"] = hloop.ParentId ?? "",
            ["level_code"] = hloop.LevelCode ?? "",
            ["child_code"] = hloop.Specification?.LevelCode ?? ""
        };

        var segments = new List<Dictionary<string, object>>();
        ParseContainerSegments(hloop, segments);
        data["segments"] = segments;

        // Extract key segments for easier access
        ExtractNamedSegments(segments, data);

        var children = new List<Dictionary<string, object>>();
        foreach (var child in hloop.HLoops)
        {
            children.Add(BuildHLoopData(child));
        }
        data["children"] = children;

        return data;
    }

    private void ExtractNamedSegments(List<Dictionary<string, object>> segments, Dictionary<string, object> data)
    {
        foreach (var seg in segments)
        {
            if (!seg.TryGetValue("id", out var idObj)) continue;
            var id = idObj?.ToString() ?? "";

            switch (id)
            {
                case "BHT":
                    data["bht"] = seg;
                    break;
                case "NM1":
                    var entityCode = seg.TryGetValue("e01", out var ec) ? ec?.ToString() : "";
                    data[$"nm1_{entityCode}"] = seg;
                    if (!data.ContainsKey("nm1_list"))
                        data["nm1_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["nm1_list"]).Add(seg);
                    break;
                case "UM":
                    data["um"] = seg;
                    break;
                case "HCR":
                    data["hcr"] = seg;
                    break;
                case "HI":
                    if (!data.ContainsKey("hi_list"))
                        data["hi_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["hi_list"]).Add(seg);
                    break;
                case "HSD":
                    data["hsd"] = seg;
                    break;
                case "DTP":
                    if (!data.ContainsKey("dtp_list"))
                        data["dtp_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["dtp_list"]).Add(seg);
                    break;
                case "REF":
                    if (!data.ContainsKey("ref_list"))
                        data["ref_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["ref_list"]).Add(seg);
                    break;
                case "TRN":
                    data["trn"] = seg;
                    break;
                case "SV1":
                case "SV2":
                    if (!data.ContainsKey("sv_list"))
                        data["sv_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["sv_list"]).Add(seg);
                    break;
                case "DMG":
                    data["dmg"] = seg;
                    break;
                case "N3":
                    data["n3"] = seg;
                    break;
                case "N4":
                    data["n4"] = seg;
                    break;
                case "PER":
                    data["per"] = seg;
                    break;
                case "PRV":
                    data["prv"] = seg;
                    break;
                case "CL1":
                    data["cl1"] = seg;
                    break;
                case "CR1":
                case "CR2":
                case "CR5":
                case "CR6":
                    data[$"{id.ToLower()}"] = seg;
                    break;
                case "PWK":
                    if (!data.ContainsKey("pwk_list"))
                        data["pwk_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["pwk_list"]).Add(seg);
                    break;
                case "MSG":
                    data["msg"] = seg;
                    break;
                case "SBR":
                    data["sbr"] = seg;
                    break;
                case "INS":
                    data["ins"] = seg;
                    break;
                case "EB":
                    if (!data.ContainsKey("eb_list"))
                        data["eb_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["eb_list"]).Add(seg);
                    break;
                case "STC":
                    if (!data.ContainsKey("stc_list"))
                        data["stc_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["stc_list"]).Add(seg);
                    break;
                case "CAS":
                    if (!data.ContainsKey("cas_list"))
                        data["cas_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["cas_list"]).Add(seg);
                    break;
                case "CLP":
                    data["clp"] = seg;
                    break;
                case "AMT":
                    if (!data.ContainsKey("amt_list"))
                        data["amt_list"] = new List<Dictionary<string, object>>();
                    ((List<Dictionary<string, object>>)data["amt_list"]).Add(seg);
                    break;
            }
        }
    }
}

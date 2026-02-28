using OopFactory.X12.FhirConverter.Engine;
using OopFactory.X12.FhirConverter.Engine.CustomFilters;
using Xunit;

namespace OopFactory.X12.FhirConverter.Tests.X12ToFhir;

public class PriorAuth278Tests
{
    [Fact]
    public void DateFilters_X12Date_ShouldConvertCorrectly()
    {
        Assert.Equal("2025-04-30", DateFilters.X12Date("20250430"));
        Assert.Equal("2025-04", DateFilters.X12Date("202504"));
        Assert.Equal("", DateFilters.X12Date(""));
    }

    [Fact]
    public void DateFilters_FhirDateToX12_ShouldConvertCorrectly()
    {
        Assert.Equal("20250430", DateFilters.FhirDateToX12("2025-04-30"));
        Assert.Equal("", DateFilters.FhirDateToX12(""));
    }

    [Fact]
    public void DateFilters_X12DateRange_ShouldParseBothParts()
    {
        Assert.Equal("2025-05-02", DateFilters.X12DateRangeStart("20250502-20250602"));
        Assert.Equal("2025-06-02", DateFilters.X12DateRangeEnd("20250502-20250602"));
    }

    [Fact]
    public void FhirFilters_X12Gender_ShouldMapCorrectly()
    {
        Assert.Equal("male", FhirFilters.X12Gender("M"));
        Assert.Equal("female", FhirFilters.X12Gender("F"));
        Assert.Equal("unknown", FhirFilters.X12Gender("U"));
        Assert.Equal("unknown", FhirFilters.X12Gender(null));
    }

    [Fact]
    public void FhirFilters_GenerateUuid_ShouldBeDeterministic()
    {
        var uuid1 = FhirFilters.GenerateUuid("Patient", "12345");
        var uuid2 = FhirFilters.GenerateUuid("Patient", "12345");
        Assert.Equal(uuid1, uuid2);

        var uuid3 = FhirFilters.GenerateUuid("Patient", "67890");
        Assert.NotEqual(uuid1, uuid3);
    }

    [Fact]
    public void FhirFilters_X12RelationshipToFhir_ShouldMapCorrectly()
    {
        Assert.Equal("self", FhirFilters.X12RelationshipToFhir("18"));
        Assert.Equal("spouse", FhirFilters.X12RelationshipToFhir("01"));
        Assert.Equal("child", FhirFilters.X12RelationshipToFhir("19"));
    }

    [Fact]
    public void CodeMappingFilters_DiagnosisCodeSystem_ShouldReturnCorrectUri()
    {
        Assert.Equal("http://hl7.org/fhir/sid/icd-10-cm", CodeMappingFilters.DiagnosisCodeSystem("ABK"));
        Assert.Equal("http://hl7.org/fhir/sid/icd-9-cm", CodeMappingFilters.DiagnosisCodeSystem("BK"));
    }

    [Fact]
    public void CodeMappingFilters_ProcedureCodeSystem_ShouldReturnCorrectUri()
    {
        Assert.Equal("http://www.ama-assn.org/go/cpt", CodeMappingFilters.ProcedureCodeSystem("HC"));
        Assert.Equal("https://www.cms.gov/Medicare/Coding/HCPCSReleaseCodeSets", CodeMappingFilters.ProcedureCodeSystem("HP"));
    }

    [Fact]
    public void X12Filters_SubElement_ShouldExtractCorrectly()
    {
        Assert.Equal("BF", X12Filters.SubElement("BF:41090:D8:20050430", 1));
        Assert.Equal("41090", X12Filters.SubElement("BF:41090:D8:20050430", 2));
        Assert.Equal("D8", X12Filters.SubElement("BF:41090:D8:20050430", 3));
    }

    [Fact]
    public void X12Filters_EntityToResourceType_ShouldMapCorrectly()
    {
        Assert.Equal("Patient", X12Filters.EntityToResourceType("IL"));
        Assert.Equal("Practitioner", X12Filters.EntityToResourceType("85"));
        Assert.Equal("Organization", X12Filters.EntityToResourceType("PR"));
        Assert.Equal("Practitioner", X12Filters.EntityToResourceType("1P"));
    }

    [Fact]
    public void ConversionResult_Success_ShouldSetProperties()
    {
        var result = ConversionResult.Success("{}", "278");
        Assert.True(result.IsSuccess);
        Assert.Equal("{}", result.Output);
        Assert.Equal("278", result.TransactionType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ConversionResult_Error_ShouldSetProperties()
    {
        var result = ConversionResult.Error("test error");
        Assert.False(result.IsSuccess);
        Assert.Equal("test error", result.ErrorMessage);
    }
}

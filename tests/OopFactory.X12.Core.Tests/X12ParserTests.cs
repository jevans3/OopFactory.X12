using OopFactory.X12.Parsing;
using OopFactory.X12.Parsing.Model;
using Xunit;

namespace OopFactory.X12.Core.Tests;

public class X12ParserTests
{
    private readonly X12Parser _parser = new();

    [Fact]
    public void Parse278Request_ShouldProduceValidInterchange()
    {
        var x12 = File.ReadAllText("TestData/278/Example_3_1_1_Request.txt");
        var interchanges = _parser.ParseMultiple(x12);

        Assert.Single(interchanges);
        var interchange = interchanges[0];
        Assert.NotNull(interchange);

        var fg = interchange.FunctionGroups.First();
        Assert.Equal("HI", fg.FunctionalIdentifierCode);

        var transaction = fg.Transactions.First();
        Assert.Equal("278", transaction.IdentifierCode);
    }

    [Fact]
    public void Parse278Response_ShouldContainHCRSegment()
    {
        var x12 = File.ReadAllText("TestData/278/Example_3_1_2_Response.txt");
        var interchanges = _parser.ParseMultiple(x12);

        Assert.Single(interchanges);
        var xml = interchanges[0].Serialize();
        Assert.Contains("HCR", xml);
        Assert.Contains("A1", xml); // Certified in total
        Assert.Contains("AUTH0001", xml); // Auth number
    }

    [Fact]
    public void Parse278_ShouldSerializeToXml()
    {
        var x12 = File.ReadAllText("TestData/278/Example_3_1_1_Request.txt");
        var interchanges = _parser.ParseMultiple(x12);
        var xml = interchanges[0].Serialize();

        Assert.NotEmpty(xml);
        Assert.Contains("<Interchange", xml);
        Assert.Contains("<Transaction", xml);
        Assert.Contains("278", xml);
    }

    [Fact]
    public void Parse276_ShouldProduceValidInterchange()
    {
        var x12 = File.ReadAllText("TestData/276/Example1_IG.txt");
        var interchanges = _parser.ParseMultiple(x12);

        Assert.Single(interchanges);
        var transaction = interchanges[0].FunctionGroups.First().Transactions.First();
        Assert.True(transaction.IdentifierCode == "276" || transaction.IdentifierCode == "277");
    }

    [Fact]
    public void Parse275_ShouldProduceValidInterchange()
    {
        var x12 = File.ReadAllText("TestData/275/FromImplementationGuide_1.txt");
        var interchanges = _parser.ParseMultiple(x12);

        Assert.Single(interchanges);
        var transaction = interchanges[0].FunctionGroups.First().Transactions.First();
        Assert.Equal("275", transaction.IdentifierCode);
    }

    [Fact]
    public void ParseMultiple278_ShouldHandleAllExamples()
    {
        var files = Directory.GetFiles("TestData/278", "*.txt");
        Assert.True(files.Length >= 2, "Expected at least 2 test files for 278");

        foreach (var file in files)
        {
            var x12 = File.ReadAllText(file);
            var interchanges = _parser.ParseMultiple(x12);
            Assert.NotEmpty(interchanges);

            var xml = interchanges[0].Serialize();
            Assert.Contains("278", xml);
        }
    }
}

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using Xunit;

namespace OopFactory.X12.FhirConverter.Api.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MetadataEndpoint_ShouldReturnCapabilityStatement()
    {
        var response = await _client.GetAsync("/api/fhir/metadata");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CapabilityStatement", content);
        Assert.Contains("4.0.1", content);
    }

    [Fact]
    public async Task TemplatesEndpoint_ShouldListTemplates()
    {
        var response = await _client.GetAsync("/api/templates");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task X12ToFhir_WithEmptyBody_ShouldReturn400()
    {
        var content = new StringContent("", Encoding.UTF8, "text/plain");
        var response = await _client.PostAsync("/api/convert/x12-to-fhir", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FhirToX12_WithoutTransactionType_ShouldReturn400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/convert/fhir-to-x12", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PasSubmit_WithEmptyBody_ShouldReturn400()
    {
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/fhir/Claim/$submit", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

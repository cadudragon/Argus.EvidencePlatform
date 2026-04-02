using System.Net;
using System.Net.Http.Json;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class ExportEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExportEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_export_should_queue_and_then_get_export_job()
    {
        using var client = _factory.CreateClient();
        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest("CASE-2026-301", "Export case", null));
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();

        var createExportResponse = await client.PostAsJsonAsync(
            "/api/exports",
            new CreateCaseExportRequest(createdCase!.Id, "zip", "Need export"));

        createExportResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var createdExport = await createExportResponse.Content.ReadFromJsonAsync<ExportJobResponse>();
        createdExport.Should().NotBeNull();
        createdExport!.CaseId.Should().Be(createdCase.Id);
        createdExport.Status.Should().Be("Queued");
        createdExport.RequestedBy.Should().Be("Local Operator");

        var getExportResponse = await client.GetAsync($"/api/exports/{createdExport.Id:D}");

        getExportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getExportResponse.Content.ReadFromJsonAsync<ExportJobResponse>();
        fetched.Should().BeEquivalentTo(createdExport);
    }

    [Fact]
    public async Task Post_export_should_return_not_found_when_case_does_not_exist()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/exports",
            new CreateCaseExportRequest(Guid.NewGuid(), "zip", "Need export"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_export_should_return_validation_problem_for_invalid_format()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/exports",
            new CreateCaseExportRequest(Guid.NewGuid(), "tar", "Need export"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_export_should_return_not_found_when_job_does_not_exist()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/exports/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

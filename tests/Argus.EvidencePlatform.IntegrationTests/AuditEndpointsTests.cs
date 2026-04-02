using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class AuditEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuditEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_audit_trail_should_return_empty_list_when_case_has_no_events()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/audit/cases/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditEntryResponse>>();
        entries.Should().NotBeNull();
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_audit_trail_should_return_case_events_in_descending_order()
    {
        using var client = _factory.CreateClient();

        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest("CASE-2026-401", "Audit trail", null));
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();
        createdCase.Should().NotBeNull();

        await Task.Delay(20);

        var artifactResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase!.Id, "device-01", "Text", "artifact body"));
        artifactResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        await Task.Delay(20);

        var exportResponse = await client.PostAsJsonAsync(
            "/api/exports",
            new CreateCaseExportRequest(createdCase.Id, "zip", "Need export"));
        exportResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var auditResponse = await client.GetAsync($"/api/audit/cases/{createdCase.Id:D}");

        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await auditResponse.Content.ReadFromJsonAsync<IReadOnlyList<AuditEntryResponse>>();
        entries.Should().NotBeNull();
        entries.Should().HaveCount(3);
        entries![0].OccurredAt.Should().BeOnOrAfter(entries[1].OccurredAt);
        entries[1].OccurredAt.Should().BeOnOrAfter(entries[2].OccurredAt);
        entries.Select(x => x.Action).Should().Contain(["CaseCreated", "EvidencePreserved", "ExportQueued"]);
        entries[0].Action.Should().Be("ExportQueued");
        entries[1].Action.Should().Be("EvidencePreserved");
        entries[2].Action.Should().Be("CaseCreated");
    }

    private static MultipartFormDataContent CreateMultipartContent(
        Guid caseId,
        string sourceId,
        string evidenceType,
        string body)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(caseId.ToString("D")), "caseId");
        content.Add(new StringContent(sourceId), "sourceId");
        content.Add(new StringContent(evidenceType), "evidenceType");
        content.Add(new StringContent("secret"), "classification");
        content.Add(new StringContent("2026-04-02T10:00:00+00:00"), "captureTimestamp");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "artifact.txt");

        return content;
    }
}

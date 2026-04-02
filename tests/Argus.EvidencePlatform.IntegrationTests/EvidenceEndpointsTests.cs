using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class EvidenceEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public EvidenceEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_artifact_should_accept_and_appear_in_timeline()
    {
        using var client = _factory.CreateClient();
        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest("CASE-2026-101", "Evidence intake", null));
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();

        var response = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase!.Id, "device-01", "Text", "artifact body"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var artifact = await response.Content.ReadFromJsonAsync<IngestArtifactResponse>();
        artifact.Should().NotBeNull();
        artifact!.CaseId.Should().Be(createdCase.Id);
        artifact.SourceId.Should().Be("device-01");
        artifact.EvidenceType.Should().Be("Text");
        artifact.Status.Should().Be("Preserved");
        artifact.SizeBytes.Should().Be(13);

        var timelineResponse = await client.GetAsync($"/api/evidence/cases/{createdCase.Id:D}/timeline");

        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>();
        timeline.Should().NotBeNull();
        timeline.Should().ContainSingle();
        timeline![0].CaseId.Should().Be(createdCase.Id);
        timeline[0].SourceId.Should().Be("device-01");
        timeline[0].EvidenceType.Should().Be("Text");
        timeline[0].Status.Should().Be("Preserved");
    }

    [Fact]
    public async Task Post_artifact_should_return_not_found_when_case_does_not_exist()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(Guid.NewGuid(), "device-01", "Binary", "artifact body"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_artifact_should_return_validation_problem_for_invalid_evidence_type()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(Guid.NewGuid(), "device-01", "Audio", "artifact body"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_timeline_should_return_empty_list_when_case_has_no_evidence()
    {
        using var client = _factory.CreateClient();
        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest("CASE-2026-102", "No evidence yet", null));
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();

        var response = await client.GetAsync($"/api/evidence/cases/{createdCase!.Id:D}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await response.Content.ReadFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>();
        timeline.Should().NotBeNull();
        timeline.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_timeline_should_return_items_ordered_by_capture_then_received_desc()
    {
        using var client = _factory.CreateClient();
        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest("CASE-2026-103", "Ordering", null));
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();
        var caseId = createdCase!.Id;

        var firstResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(caseId, "device-01", "Text", "older", "2026-04-01T11:00:00+00:00"));
        var secondResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(caseId, "device-01", "Text", "newer", "2026-04-01T12:00:00+00:00"));
        var thirdResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(caseId, "device-01", "Text", "same-capture-later-received", "2026-04-01T12:00:00+00:00"));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        thirdResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var secondArtifact = await secondResponse.Content.ReadFromJsonAsync<IngestArtifactResponse>();
        var thirdArtifact = await thirdResponse.Content.ReadFromJsonAsync<IngestArtifactResponse>();

        var timelineResponse = await client.GetAsync($"/api/evidence/cases/{caseId:D}/timeline");

        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>();
        timeline.Should().NotBeNull();
        timeline.Should().HaveCount(3);
        timeline![0].CaptureTimestamp.Should().Be(timeline[1].CaptureTimestamp);
        timeline[0].ReceivedAt.Should().BeAfter(timeline[1].ReceivedAt);
        timeline[0].Id.Should().Be(thirdArtifact!.EvidenceId);
        timeline[1].Id.Should().Be(secondArtifact!.EvidenceId);
        timeline[2].CaptureTimestamp.Should().BeBefore(timeline[1].CaptureTimestamp);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        Guid caseId,
        string sourceId,
        string evidenceType,
        string body,
        string captureTimestamp = "2026-04-01T11:45:00+00:00")
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(caseId.ToString("D")), "caseId");
        content.Add(new StringContent(sourceId), "sourceId");
        content.Add(new StringContent(evidenceType), "evidenceType");
        content.Add(new StringContent("secret"), "classification");
        content.Add(new StringContent(captureTimestamp), "captureTimestamp");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "artifact.txt");

        return content;
    }
}

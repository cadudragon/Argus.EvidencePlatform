using System.Net;
using System.Net.Http.Json;
using System.Text;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class EvidenceReadEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public EvidenceReadEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_artifacts_should_return_paged_items_with_download_url()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-READ-001");

        await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase.Id, "device-01", "Text", "artifact-1"));
        await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase.Id, "device-01", "Text", "artifact-2"));

        var response = await client.GetAsync($"/api/evidence/cases/{createdCase.Id:D}/artifacts?pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ListCaseArtifactsResponse>();
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();
        payload.Items[0].CaseId.Should().Be(createdCase.Id);
        payload.Items[0].HasBinary.Should().BeTrue();
        payload.Items[0].DownloadUrl.Should().Be($"/api/evidence/artifacts/{payload.Items[0].Id:D}/content");
        payload.NextCursor.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_artifacts_should_return_bad_request_for_invalid_cursor()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/evidence/cases/{Guid.NewGuid():D}/artifacts?cursor=not-base64");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_artifacts_should_return_bad_request_for_invalid_page_size()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/evidence/cases/{Guid.NewGuid():D}/artifacts?pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_artifact_content_should_stream_blob_content()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-READ-002");

        var ingestResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase.Id, "device-01", "Text", "artifact-body"));
        var artifact = await ingestResponse.Content.ReadFromJsonAsync<IngestArtifactResponse>();

        var response = await client.GetAsync($"/api/evidence/artifacts/{artifact!.EvidenceId:D}/content");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await response.Content.ReadAsStringAsync()).Should().Be("artifact-body");
    }

    [Fact]
    public async Task Get_artifact_content_should_support_range_requests()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-READ-003");

        var ingestResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase.Id, "device-01", "Text", "artifact-body"));
        var artifact = await ingestResponse.Content.ReadFromJsonAsync<IngestArtifactResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/evidence/artifacts/{artifact!.EvidenceId:D}/content");
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 7);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        (await response.Content.ReadAsStringAsync()).Should().Be("artifact");
    }

    [Fact]
    public async Task Get_artifact_content_should_return_not_found_for_missing_artifact()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/evidence/artifacts/{Guid.NewGuid():D}/content");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_artifact_content_should_return_conflict_when_blob_is_missing()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-READ-004");

        var ingestResponse = await client.PostAsync(
            "/api/evidence/artifacts",
            CreateMultipartContent(createdCase.Id, "device-01", "Text", "artifact-body"));
        var artifact = await ingestResponse.Content.ReadFromJsonAsync<IngestArtifactResponse>();

        var timeline = await client.GetFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>(
            $"/api/evidence/cases/{createdCase.Id:D}/timeline");
        timeline.Should().NotBeNull();
        _factory.BlobStore.Delete("staging", timeline![0].BlobName).Should().BeTrue();

        var response = await client.GetAsync($"/api/evidence/artifacts/{artifact!.EvidenceId:D}/content");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string externalCaseId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "Evidence read", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CaseResponse>())!;
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
        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "artifact.txt");

        return content;
    }
}

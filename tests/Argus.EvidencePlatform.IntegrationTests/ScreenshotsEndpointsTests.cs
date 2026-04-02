using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Contracts.Screenshots;
using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class ScreenshotsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ScreenshotsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_screenshots_should_accept_gzip_multipart_and_appear_in_timeline()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-701");
        await SeedDeviceSourceAsync(createdCase.Id, createdCase.ExternalCaseId, "android-screen-01", DateTimeOffset.UtcNow.AddMinutes(30));

        using var response = await client.PostAsync(
            "/api/screenshots",
            await CreateGzipMultipartContentAsync(
                "android-screen-01",
                createdCase.ExternalCaseId,
                "1775156400000",
                "capture-bytes"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<IngestScreenshotResponse>();
        payload.Should().NotBeNull();
        payload!.CaseId.Should().Be(createdCase.ExternalCaseId);
        payload.DeviceId.Should().Be("android-screen-01");
        payload.Status.Should().Be("Preserved");

        var timelineResponse = await client.GetAsync($"/api/evidence/cases/{createdCase.Id:D}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>();
        timeline.Should().NotBeNull();
        timeline.Should().ContainSingle();
        timeline![0].SourceId.Should().Be("android-screen-01");
        timeline[0].EvidenceType.Should().Be("Image");
        timeline[0].CaptureTimestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1775156400000));
    }

    [Fact]
    public async Task Post_screenshots_should_return_bad_request_when_gzip_is_missing()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-702");
        await SeedDeviceSourceAsync(createdCase.Id, createdCase.ExternalCaseId, "android-screen-02", DateTimeOffset.UtcNow.AddMinutes(30));

        using var response = await client.PostAsync(
            "/api/screenshots",
            CreatePlainMultipartContent(
                "android-screen-02",
                createdCase.ExternalCaseId,
                "1775156400000",
                "capture-bytes"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_screenshots_should_return_conflict_when_device_does_not_belong_to_case()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-703");
        var otherCase = await CreateCaseAsync(client, "CASE-2026-704");
        await SeedDeviceSourceAsync(otherCase.Id, otherCase.ExternalCaseId, "android-screen-03", DateTimeOffset.UtcNow.AddMinutes(30));

        using var response = await client.PostAsync(
            "/api/screenshots",
            await CreateGzipMultipartContentAsync(
                "android-screen-03",
                createdCase.ExternalCaseId,
                "1775156400000",
                "capture-bytes"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string externalCaseId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "Screenshots", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCase = await response.Content.ReadFromJsonAsync<CaseResponse>();
        return createdCase!;
    }

    private async Task SeedDeviceSourceAsync(Guid caseId, string caseExternalId, string deviceId, DateTimeOffset validUntil)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeviceSourceRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repository.AddAsync(
            DeviceSource.Register(
                Guid.NewGuid(),
                deviceId,
                caseId,
                caseExternalId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                validUntil),
            CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static MultipartFormDataContent CreatePlainMultipartContent(
        string deviceId,
        string caseId,
        string captureTimestamp,
        string body)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(deviceId), "deviceId");
        content.Add(new StringContent(Sha256Hex(body)), "sha256");
        content.Add(new StringContent(caseId), "caseId");
        content.Add(new StringContent(captureTimestamp), "captureTimestamp");

        var imageContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image", "capture.jpg");

        return content;
    }

    private static async Task<ByteArrayContent> CreateGzipMultipartContentAsync(
        string deviceId,
        string caseId,
        string captureTimestamp,
        string body)
    {
        using var multipart = CreatePlainMultipartContent(deviceId, caseId, captureTimestamp, body);
        await using var output = new MemoryStream();
        await multipart.CopyToAsync(output);
        var multipartBytes = output.ToArray();

        await using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await gzip.WriteAsync(multipartBytes);
        }

        var content = new ByteArrayContent(compressed.ToArray());
        foreach (var header in multipart.Headers)
        {
            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        content.Headers.ContentEncoding.Add("gzip");
        return content;
    }

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}

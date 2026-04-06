using System.Net;
using System.Net.Http.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.TextCaptures;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class TextCapturesEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public TextCapturesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_text_captures_should_persist_batch_and_write_audit_entry()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-811");
        await SeedDeviceSourceAsync(createdCase.Id, createdCase.ExternalCaseId, "android-text-01", DateTimeOffset.UtcNow.AddMinutes(30));

        var response = await client.PostAsJsonAsync(
            "/api/text-captures",
            new IngestTextCaptureRequest(
                "android-text-01",
                createdCase.ExternalCaseId,
                "3f786850e387550fdab836ed7e6dc881de23001b",
                1775156400000,
                [
                    new TextCaptureItemRequest("com.whatsapp", "android.widget.TextView", "Message content", null),
                    new TextCaptureItemRequest("com.signal", "android.widget.TextView", null, "Description")
                ]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArgusDbContext>();
        var storedBatch = dbContext.TextCaptureBatches.Single();
        storedBatch.CaseId.Should().Be(createdCase.Id);
        storedBatch.CaseExternalId.Should().Be(createdCase.ExternalCaseId);
        storedBatch.DeviceId.Should().Be("android-text-01");
        storedBatch.CaptureCount.Should().Be(2);

        var auditResponse = await client.GetAsync($"/api/audit/cases/{createdCase.Id:D}");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditEntries = await auditResponse.Content.ReadFromJsonAsync<IReadOnlyList<AuditEntryResponse>>();
        auditEntries.Should().ContainSingle(entry => entry.Action == "TextCaptureBatchCaptured" && entry.ActorId == "android-text-01");
    }

    [Fact]
    public async Task Post_text_captures_should_return_bad_request_for_invalid_payload()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/text-captures",
            new IngestTextCaptureRequest(
                "android-text-02",
                "CASE-2026-812",
                "sha",
                0,
                []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_text_captures_should_return_conflict_when_device_does_not_belong_to_case()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-813");
        var otherCase = await CreateCaseAsync(client, "CASE-2026-814");
        await SeedDeviceSourceAsync(otherCase.Id, otherCase.ExternalCaseId, "android-text-03", DateTimeOffset.UtcNow.AddMinutes(30));

        var response = await client.PostAsJsonAsync(
            "/api/text-captures",
            new IngestTextCaptureRequest(
                "android-text-03",
                createdCase.ExternalCaseId,
                "3f786850e387550fdab836ed7e6dc881de23001b",
                1775156400000,
                [
                    new TextCaptureItemRequest("com.whatsapp", "android.widget.TextView", "Message content", null)
                ]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string externalCaseId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "TextCaptures", null));

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
}

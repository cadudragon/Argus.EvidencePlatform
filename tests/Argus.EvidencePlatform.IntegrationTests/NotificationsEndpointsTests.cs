using System.Net;
using System.Net.Http.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Notifications;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class NotificationsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public NotificationsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_notifications_should_persist_capture_and_write_audit_entry()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-801");
        await SeedDeviceSourceAsync(createdCase.Id, createdCase.ExternalCaseId, "android-notification-01", DateTimeOffset.UtcNow.AddMinutes(30));

        var response = await client.PostAsJsonAsync(
            "/api/notifications",
            new IngestNotificationRequest(
                "android-notification-01",
                createdCase.ExternalCaseId,
                "3f786850e387550fdab836ed7e6dc881de23001b",
                1775156400000,
                "com.whatsapp",
                "Sender",
                "Message preview",
                "Expanded message preview",
                1775156405000,
                "msg"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArgusDbContext>();
        var storedNotification = dbContext.NotificationCaptures.Single();
        storedNotification.CaseId.Should().Be(createdCase.Id);
        storedNotification.CaseExternalId.Should().Be(createdCase.ExternalCaseId);
        storedNotification.DeviceId.Should().Be("android-notification-01");
        storedNotification.PackageName.Should().Be("com.whatsapp");
        storedNotification.Text.Should().Be("Message preview");
        storedNotification.BigText.Should().Be("Expanded message preview");
        storedNotification.NotificationTimestamp.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1775156405000));

        var auditResponse = await client.GetAsync($"/api/audit/cases/{createdCase.Id:D}");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditEntries = await auditResponse.Content.ReadFromJsonAsync<IReadOnlyList<AuditEntryResponse>>();
        auditEntries.Should().ContainSingle(entry => entry.Action == "NotificationCaptured" && entry.ActorId == "android-notification-01");
    }

    [Fact]
    public async Task Post_notifications_should_return_bad_request_for_invalid_payload()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/notifications",
            new IngestNotificationRequest(
                "android-notification-02",
                "CASE-2026-802",
                "sha",
                0,
                string.Empty,
                null,
                null,
                null,
                0,
                null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_notifications_should_return_conflict_when_device_does_not_belong_to_case()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-803");
        var otherCase = await CreateCaseAsync(client, "CASE-2026-804");
        await SeedDeviceSourceAsync(otherCase.Id, otherCase.ExternalCaseId, "android-notification-03", DateTimeOffset.UtcNow.AddMinutes(30));

        var response = await client.PostAsJsonAsync(
            "/api/notifications",
            new IngestNotificationRequest(
                "android-notification-03",
                createdCase.ExternalCaseId,
                "3f786850e387550fdab836ed7e6dc881de23001b",
                1775156400000,
                "com.whatsapp",
                "Sender",
                "Message preview",
                null,
                1775156405000,
                "msg"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string externalCaseId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "Notifications", null));

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

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Device;
using Argus.EvidencePlatform.Contracts.Enrollment;
using Argus.EvidencePlatform.Domain.Enrollment;
using Argus.EvidencePlatform.Infrastructure.Firebase;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class DeviceEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public DeviceEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_pong_should_return_ok_for_active_device()
    {
        using var client = _factory.CreateClient();
        await ActivateDeviceAsync(client, "CASE-2026-601", "423456789", "android-device-pong-ok");

        var response = await client.PostAsJsonAsync("/api/pong", new PongRequest("android-device-pong-ok"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_pong_should_return_gone_for_unknown_device()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/pong", new PongRequest("android-device-pong-missing"));

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Put_fcm_token_should_return_ok_for_active_device()
    {
        using var client = _factory.CreateClient();
        await ActivateDeviceAsync(client, "CASE-2026-602", "523456789", "android-device-fcm-ok");

        var response = await client.PutAsJsonAsync(
            "/api/fcm-token",
            CreateFcmTokenRequest("android-device-fcm-ok", "fcm-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_fcm_token_should_return_gone_for_unknown_device()
    {
        using var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/fcm-token",
            CreateFcmTokenRequest("android-device-fcm-missing", "fcm-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Put_fcm_token_should_return_bad_request_when_command_key_is_missing()
    {
        using var client = _factory.CreateClient();
        await ActivateDeviceAsync(client, "CASE-2026-605", "823456789", "android-device-fcm-missing-key");

        var response = await client.PutAsJsonAsync(
            "/api/fcm-token",
            new
            {
                deviceId = "android-device-fcm-missing-key",
                fcmToken = "fcm-token"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_fcm_token_should_return_bad_request_when_command_key_public_key_is_invalid()
    {
        using var client = _factory.CreateClient();
        await ActivateDeviceAsync(client, "CASE-2026-606", "923456789", "android-device-fcm-invalid-key");

        var response = await client.PutAsJsonAsync(
            "/api/fcm-token",
            new UpdateFcmTokenRequest(
                "android-device-fcm-invalid-key",
                "fcm-token",
                new FcmCommandKeyRequest("ECDH-P256", "device-ecdh-invalid", "not-a-key")));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_device_commands_screenshot_should_return_accepted_for_active_device_with_fcm_token()
    {
        using var client = _factory.CreateClient();
        _factory.DeviceCommandDispatcher.ScreenshotRequests.Clear();
        _factory.DeviceCommandDispatcher.NextResult = new(DeviceCommandDispatchStatus.Success, "firebase-message-1");
        await ActivateDeviceAsync(client, "CASE-2026-603", "623456789", "android-device-shot-ok");
        await client.PutAsJsonAsync(
            "/api/fcm-token",
            CreateFcmTokenRequest("android-device-shot-ok", "fcm-token-shot"));

        var response = await client.PostAsJsonAsync(
            "/api/device-commands/screenshot",
            new RequestScreenshotCommandRequest("android-device-shot-ok"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<RequestScreenshotCommandResponse>();
        payload.Should().NotBeNull();
        payload!.DeviceId.Should().Be("android-device-shot-ok");
        payload.CaseId.Should().Be("CASE-2026-603");
        payload.MessageId.Should().Be("firebase-message-1");
        _factory.DeviceCommandDispatcher.ScreenshotRequests.Should().ContainSingle();
        _factory.DeviceCommandDispatcher.ScreenshotRequests.Single().DeviceCommandKey.Kid.Should().Be("device-ecdh-android-device-shot-ok");
        _factory.DeviceCommandDispatcher.ScreenshotRequests.Single().Command.Should().Be("screenshot");
    }

    [Fact]
    public async Task Post_device_commands_screenshot_should_return_not_found_when_fcm_token_is_missing()
    {
        using var client = _factory.CreateClient();
        await ActivateDeviceAsync(client, "CASE-2026-604", "723456789", "android-device-shot-missing-token");

        var response = await client.PostAsJsonAsync(
            "/api/device-commands/screenshot",
            new RequestScreenshotCommandRequest("android-device-shot-missing-token"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task ActivateDeviceAsync(
        HttpClient client,
        string caseExternalId,
        string token,
        string deviceId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(caseExternalId, "Device", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCase = await response.Content.ReadFromJsonAsync<CaseResponse>();

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IActivationTokenRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repository.AddAsync(
            ActivationToken.Issue(
                Guid.NewGuid(),
                token,
                createdCase!.Id,
                createdCase.ExternalCaseId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddMinutes(30)),
            CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var activateResponse = await client.PostAsJsonAsync("/api/activate", new ActivationRequest(token, deviceId));
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static UpdateFcmTokenRequest CreateFcmTokenRequest(string deviceId, string fcmToken)
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = FcmCommandEnvelopeEncryptor.EncodeBase64UrlNoPadding(ecdh.ExportSubjectPublicKeyInfo());
        return new UpdateFcmTokenRequest(
            deviceId,
            fcmToken,
            new FcmCommandKeyRequest("ECDH-P256", $"device-ecdh-{deviceId}", publicKey));
    }
}

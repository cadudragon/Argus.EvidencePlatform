using System.Security.Cryptography;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluentAssertions;
using System.Net;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class HealthEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HealthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_endpoint_should_return_success()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Protected_endpoint_should_be_accessible_with_mock_auth_in_testing()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"argus-evidence-platform-tests-{Guid.NewGuid():N}";

    public TestDeviceCommandDispatcher DeviceCommandDispatcher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Mock",
                ["Infrastructure:UseInMemoryPersistence"] = "true",
                ["Infrastructure:InMemoryDatabaseName"] = _databaseName,
                ["Wolverine:AutoProvision"] = "false",
                ["ConnectionStrings:postgresdb"] = "Host=localhost;Port=5432;Database=argus_evidence_platform_test;Username=postgres;Password=postgres",
                ["ConnectionStrings:blobs"] = "UseDevelopmentStorage=true",
                ["Firebase:Apps:0:Key"] = "fb-local-primary",
                ["Firebase:Apps:0:DisplayName"] = "Local Primary",
                ["Firebase:Apps:0:ProjectId"] = "argus-local-primary",
                ["Firebase:Apps:0:ServiceAccountPath"] = "C:\\secrets\\argus-local-primary.json",
                ["Firebase:Apps:0:IsActiveForNewCases"] = "true"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStagingService>();
            services.AddSingleton<IBlobStagingService, TestBlobStagingService>();
            services.RemoveAll<IDeviceCommandDispatcher>();
            services.AddSingleton<IDeviceCommandDispatcher>(DeviceCommandDispatcher);
        });
    }
}

internal sealed class TestBlobStagingService : IBlobStagingService
{
    public async Task<StagedBlobDescriptor> StageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        var bytes = memoryStream.ToArray();
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        return new StagedBlobDescriptor(
            "staging",
            $"{Guid.NewGuid():N}/{originalFileName}",
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            bytes.LongLength,
            sha256,
            Guid.NewGuid().ToString("N"));
    }
}

public sealed class TestDeviceCommandDispatcher : IDeviceCommandDispatcher
{
    public List<(string DeviceId, string FcmToken)> ScreenshotRequests { get; } = [];

    public DeviceCommandDispatchResult NextResult { get; set; } =
        new(DeviceCommandDispatchStatus.Success, "test-message-id");

    public Task<DeviceCommandDispatchResult> RequestScreenshotAsync(
        Guid firebaseAppId,
        string deviceId,
        string fcmToken,
        CancellationToken cancellationToken)
    {
        ScreenshotRequests.Add((deviceId, fcmToken));
        return Task.FromResult(NextResult);
    }
}

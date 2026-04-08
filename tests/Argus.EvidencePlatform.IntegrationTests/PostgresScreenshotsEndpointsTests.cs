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
using Argus.EvidencePlatform.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class PostgresScreenshotsEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("argus_bb073_api_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ArgusDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var dbContext = new ArgusDbContext(options);
        await dbContext.Database.MigrateAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Post_screenshots_should_accept_gzip_multipart_on_postgres_after_migrations()
    {
        await using var factory = new PostgresApiWebApplicationFactory(_postgres.GetConnectionString());
        using var client = factory.CreateClient();
        var externalCaseId = $"CASE-2026-PG-{Guid.NewGuid():N}";

        var createCaseResponse = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "Screenshots", null));
        createCaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseResponse>();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IDeviceSourceRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await repository.AddAsync(
                DeviceSource.Register(
                    Guid.NewGuid(),
                    "android-screen-pg-01",
                    createdCase!.Id,
                    createdCase.ExternalCaseId,
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    DateTimeOffset.UtcNow.AddMinutes(30)),
                CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        using var response = await client.PostAsync(
            "/api/screenshots",
            await CreateGzipMultipartContentAsync(
                "android-screen-pg-01",
                createdCase!.ExternalCaseId,
                "1775156400000",
                "capture-bytes"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<IngestScreenshotResponse>();
        payload.Should().NotBeNull();
        payload!.CaseId.Should().Be(createdCase.ExternalCaseId);
        payload.DeviceId.Should().Be("android-screen-pg-01");

        var timelineResponse = await client.GetAsync($"/api/evidence/cases/{createdCase.Id:D}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await timelineResponse.Content.ReadFromJsonAsync<IReadOnlyList<EvidenceTimelineItemResponse>>();
        timeline.Should().ContainSingle();
        timeline![0].EvidenceType.Should().Be("Image");
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

        await using var compressed = new MemoryStream();
        await using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await gzip.WriteAsync(output.ToArray());
        }

        var content = new ByteArrayContent(compressed.ToArray());
        foreach (var header in multipart.Headers)
        {
            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        content.Headers.ContentEncoding.Add("gzip");
        return content;
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

    private static string Sha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}

internal sealed class PostgresApiWebApplicationFactory(string postgresConnectionString) : WebApplicationFactory<Program>
{
    public TestDeviceCommandDispatcher DeviceCommandDispatcher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:postgresdb", postgresConnectionString);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "Mock",
                ["Infrastructure:UseInMemoryPersistence"] = "false",
                ["Infrastructure:BootstrapOnStartup"] = "false",
                ["Wolverine:AutoProvision"] = "false",
                ["ConnectionStrings:postgresdb"] = postgresConnectionString,
                ["Firebase:Enabled"] = "false",
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

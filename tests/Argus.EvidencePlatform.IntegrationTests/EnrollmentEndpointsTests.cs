using System.Net;
using System.Net.Http.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Enrollment;
using Argus.EvidencePlatform.Domain.Enrollment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class EnrollmentEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public EnrollmentEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_activate_should_return_ok_for_valid_token()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-501");
        var validUntil = DateTimeOffset.UtcNow.AddMinutes(30);
        await SeedActivationTokenAsync("123456789", createdCase.Id, createdCase.ExternalCaseId, validUntil);

        var response = await client.PostAsJsonAsync(
            "/api/activate",
            new ActivationRequest("123456789", "android-0123456789abcdef"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ActivationSuccessResponse>();
        payload.Should().NotBeNull();
        payload!.CaseId.Should().Be(createdCase.ExternalCaseId);
        payload.ValidUntil.Should().Be(validUntil.ToUnixTimeMilliseconds());
        payload.Scope.Should().BeEquivalentTo(["screenshot", "notification", "text"]);
    }

    [Fact]
    public async Task Post_activate_should_return_not_found_for_unknown_token()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/activate",
            new ActivationRequest("923456789", "android-unknown-token-device"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_activate_should_return_gone_for_expired_token()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-502");
        await SeedActivationTokenAsync(
            "223456789",
            createdCase.Id,
            createdCase.ExternalCaseId,
            DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await client.PostAsJsonAsync(
            "/api/activate",
            new ActivationRequest("223456789", "android-0123456789abcdef"));

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task Post_activate_should_return_gone_when_token_is_reused()
    {
        using var client = _factory.CreateClient();
        var createdCase = await CreateCaseAsync(client, "CASE-2026-503");
        await SeedActivationTokenAsync(
            "323456789",
            createdCase.Id,
            createdCase.ExternalCaseId,
            DateTimeOffset.UtcNow.AddMinutes(30));

        var first = await client.PostAsJsonAsync(
            "/api/activate",
            new ActivationRequest("323456789", "android-0123456789abcdef"));
        var second = await client.PostAsJsonAsync(
            "/api/activate",
            new ActivationRequest("323456789", "android-0123456789abcdef"));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    private static async Task<CaseResponse> CreateCaseAsync(HttpClient client, string externalCaseId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/cases",
            new CreateCaseRequest(externalCaseId, "Enrollment", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCase = await response.Content.ReadFromJsonAsync<CaseResponse>();
        return createdCase!;
    }

    private async Task SeedActivationTokenAsync(
        string token,
        Guid caseId,
        string caseExternalId,
        DateTimeOffset validUntil)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IActivationTokenRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repository.AddAsync(
            ActivationToken.Issue(
                Guid.NewGuid(),
                token,
                caseId,
                caseExternalId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                validUntil),
            CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}

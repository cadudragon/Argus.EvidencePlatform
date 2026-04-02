using System.Net;
using System.Net.Http.Json;
using Argus.EvidencePlatform.Contracts.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class CaseEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public CaseEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_case_should_create_and_then_get_case()
    {
        using var client = _factory.CreateClient();
        var request = new CreateCaseRequest("CASE-2026-001", "Investigation", "High priority");

        var createResponse = await client.PostAsJsonAsync("/api/cases", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CaseResponse>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/cases/{created!.Id:D}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        fetched.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Post_case_should_return_conflict_for_duplicate_external_case_id()
    {
        using var client = _factory.CreateClient();
        var first = new CreateCaseRequest("CASE-2026-002", "Investigation", null);
        var duplicate = new CreateCaseRequest("  CASE-2026-002  ", "Other title", null);

        var firstResponse = await client.PostAsJsonAsync("/api/cases", first);
        var duplicateResponse = await client.PostAsJsonAsync("/api/cases", duplicate);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_case_should_return_not_found_when_case_does_not_exist()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

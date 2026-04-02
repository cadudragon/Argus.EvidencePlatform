using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CreateCaseRequestValidatorTests
{
    private readonly CreateCaseRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var request = new CreateCaseRequest("CASE-2026-001", "Investigation", "Test case");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_external_case_id()
    {
        var request = new CreateCaseRequest(string.Empty, "Investigation", "Test case");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateCaseRequest.ExternalCaseId));
    }

    [Fact]
    public void Should_reject_external_case_id_longer_than_128_characters()
    {
        var request = new CreateCaseRequest(new string('A', 129), "Investigation", "Test case");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateCaseRequest.ExternalCaseId));
    }

    [Fact]
    public void Should_reject_empty_title()
    {
        var request = new CreateCaseRequest("CASE-2026-001", string.Empty, "Test case");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateCaseRequest.Title));
    }

    [Fact]
    public void Should_reject_title_longer_than_256_characters()
    {
        var request = new CreateCaseRequest("CASE-2026-001", new string('T', 257), "Test case");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateCaseRequest.Title));
    }

    [Fact]
    public void Should_reject_description_longer_than_2048_characters()
    {
        var request = new CreateCaseRequest("CASE-2026-001", "Investigation", new string('D', 2049));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateCaseRequest.Description));
    }
}

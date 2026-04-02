using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CreateCaseExportRequestValidatorTests
{
    private readonly CreateCaseExportRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var request = new CreateCaseExportRequest(Guid.NewGuid(), "zip", "Need export");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_accept_null_format()
    {
        var request = new CreateCaseExportRequest(Guid.NewGuid(), null, "Need export");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_case_id()
    {
        var request = new CreateCaseExportRequest(Guid.Empty, "zip", "Need export");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCaseExportRequest.CaseId));
    }

    [Fact]
    public void Should_reject_unsupported_format()
    {
        var request = new CreateCaseExportRequest(Guid.NewGuid(), "tar", "Need export");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCaseExportRequest.Format));
    }

    [Fact]
    public void Should_reject_format_longer_than_32_characters()
    {
        var request = new CreateCaseExportRequest(Guid.NewGuid(), new string('z', 33), "Need export");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCaseExportRequest.Format));
    }

    [Fact]
    public void Should_reject_reason_longer_than_512_characters()
    {
        var request = new CreateCaseExportRequest(Guid.NewGuid(), "zip", new string('r', 513));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(CreateCaseExportRequest.Reason));
    }
}

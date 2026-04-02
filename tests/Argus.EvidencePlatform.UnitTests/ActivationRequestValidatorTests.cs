using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Enrollment;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class ActivationRequestValidatorTests
{
    private readonly ActivationRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var result = _validator.Validate(new ActivationRequest("123456789", "android-0123456789abcdef"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_invalid_token()
    {
        var result = _validator.Validate(new ActivationRequest("12345", "android-0123456789abcdef"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ActivationRequest.Token));
    }
}

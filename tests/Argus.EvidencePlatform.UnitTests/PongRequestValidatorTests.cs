using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Device;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class PongRequestValidatorTests
{
    private readonly PongRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var result = _validator.Validate(new PongRequest("android-0123456789abcdef"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_device_id()
    {
        var result = _validator.Validate(new PongRequest(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(PongRequest.DeviceId));
    }
}

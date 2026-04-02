using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Device;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class UpdateFcmTokenRequestValidatorTests
{
    private readonly UpdateFcmTokenRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var result = _validator.Validate(new UpdateFcmTokenRequest("android-0123456789abcdef", "fcm-token"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_fcm_token()
    {
        var result = _validator.Validate(new UpdateFcmTokenRequest("android-0123456789abcdef", string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateFcmTokenRequest.FcmToken));
    }
}

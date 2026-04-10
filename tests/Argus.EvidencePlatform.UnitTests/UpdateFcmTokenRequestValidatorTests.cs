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
        using var keys = new FcmCommandTestKeys();
        var result = _validator.Validate(new UpdateFcmTokenRequest(
            "android-0123456789abcdef",
            "fcm-token",
            new FcmCommandKeyRequest("ECDH-P256", "device-ecdh-0123456789abcdef", keys.DevicePublicKey)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_fcm_token()
    {
        using var keys = new FcmCommandTestKeys();
        var result = _validator.Validate(new UpdateFcmTokenRequest(
            "android-0123456789abcdef",
            string.Empty,
            new FcmCommandKeyRequest("ECDH-P256", "device-ecdh-0123456789abcdef", keys.DevicePublicKey)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateFcmTokenRequest.FcmToken));
    }

    [Fact]
    public void Should_reject_invalid_command_key()
    {
        var result = _validator.Validate(new UpdateFcmTokenRequest(
            "android-0123456789abcdef",
            "fcm-token",
            new FcmCommandKeyRequest("ECDH-P256", "device-ecdh-0123456789abcdef", "not-a-key")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "FcmCommandKey.PublicKey");
    }
}

using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Notifications;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IngestNotificationRequestValidatorTests
{
    private readonly IngestNotificationRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var result = _validator.Validate(new IngestNotificationRequest(
            "android-0123456789abcdef",
            "CASE-2026-900",
            "3f786850e387550fdab836ed7e6dc881de23001b",
            1775156400000,
            "com.whatsapp",
            "Sender",
            "Message preview",
            "Expanded message preview",
            1775156400000,
            "msg"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_missing_package_name()
    {
        var result = _validator.Validate(new IngestNotificationRequest(
            "android-0123456789abcdef",
            "CASE-2026-900",
            "3f786850e387550fdab836ed7e6dc881de23001b",
            1775156400000,
            string.Empty,
            "Sender",
            "Message preview",
            null,
            1775156400000,
            "msg"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngestNotificationRequest.PackageName));
    }
}

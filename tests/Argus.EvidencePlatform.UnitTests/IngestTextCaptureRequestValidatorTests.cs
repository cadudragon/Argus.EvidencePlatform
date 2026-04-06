using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.TextCaptures;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IngestTextCaptureRequestValidatorTests
{
    private readonly IngestTextCaptureRequestValidator _validator = new();

    [Fact]
    public void Should_accept_valid_request()
    {
        var result = _validator.Validate(new IngestTextCaptureRequest(
            "android-0123456789abcdef",
            "CASE-2026-910",
            "3f786850e387550fdab836ed7e6dc881de23001b",
            1775156400000,
            [
                new TextCaptureItemRequest(
                    "com.whatsapp",
                    "android.widget.TextView",
                    "Message content",
                    null)
            ]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_captures()
    {
        var result = _validator.Validate(new IngestTextCaptureRequest(
            "android-0123456789abcdef",
            "CASE-2026-910",
            "3f786850e387550fdab836ed7e6dc881de23001b",
            1775156400000,
            []));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(IngestTextCaptureRequest.Captures));
    }
}

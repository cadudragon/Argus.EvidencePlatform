using Argus.EvidencePlatform.Domain.Enrollment;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class ActivationTokenTests
{
    [Fact]
    public void Issue_should_normalize_case_external_id_and_consume_token()
    {
        var issuedAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var validUntil = issuedAt.AddMinutes(30);
        var result = ActivationToken.Issue(
            Guid.NewGuid(),
            "123456789",
            Guid.NewGuid(),
            "  CASE-2026-301  ",
            issuedAt,
            validUntil);

        result.CaseExternalId.Should().Be("CASE-2026-301");
        result.IsConsumed.Should().BeFalse();
        result.IsExpired(issuedAt).Should().BeFalse();

        result.Consume("  android-0123456789abcdef  ", issuedAt.AddMinutes(1));

        result.IsConsumed.Should().BeTrue();
        result.ConsumedByDeviceId.Should().Be("android-0123456789abcdef");
        result.ConsumedAt.Should().Be(issuedAt.AddMinutes(1));
    }

    [Fact]
    public void Issue_should_reject_token_that_is_not_nine_digits()
    {
        var action = () => ActivationToken.Issue(
            Guid.NewGuid(),
            "12A456",
            Guid.NewGuid(),
            "CASE-2026-301",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5));

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Consume_should_reject_expired_token()
    {
        var issuedAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var token = ActivationToken.Issue(
            Guid.NewGuid(),
            "123456789",
            Guid.NewGuid(),
            "CASE-2026-301",
            issuedAt,
            issuedAt.AddMinutes(5));

        var action = () => token.Consume("android-0123456789abcdef", issuedAt.AddMinutes(5));

        action.Should().Throw<InvalidOperationException>();
    }
}

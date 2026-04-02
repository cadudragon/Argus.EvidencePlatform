using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class FcmTokenBindingTests
{
    [Fact]
    public void Bind_should_normalize_values_and_update_token()
    {
        var boundAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var result = FcmTokenBinding.Bind(
            Guid.NewGuid(),
            "  android-0123456789abcdef  ",
            "  token-001  ",
            boundAt);

        result.DeviceId.Should().Be("android-0123456789abcdef");
        result.FcmToken.Should().Be("token-001");

        result.UpdateToken("token-002", boundAt.AddMinutes(5));

        result.FcmToken.Should().Be("token-002");
        result.UpdatedAt.Should().Be(boundAt.AddMinutes(5));
    }
}

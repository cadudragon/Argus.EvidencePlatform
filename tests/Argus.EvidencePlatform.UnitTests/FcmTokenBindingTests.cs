using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class FcmTokenBindingTests
{
    [Fact]
    public void Bind_should_normalize_values_and_update_token()
    {
        var boundAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var firebaseAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nextFirebaseAppId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var result = FcmTokenBinding.Bind(
            Guid.NewGuid(),
            firebaseAppId,
            "  android-0123456789abcdef  ",
            "  token-001  ",
            boundAt);

        result.FirebaseAppId.Should().Be(firebaseAppId);
        result.DeviceId.Should().Be("android-0123456789abcdef");
        result.FcmToken.Should().Be("token-001");

        result.UpdateToken(nextFirebaseAppId, "token-002", boundAt.AddMinutes(5));

        result.FirebaseAppId.Should().Be(nextFirebaseAppId);
        result.FcmToken.Should().Be("token-002");
        result.UpdatedAt.Should().Be(boundAt.AddMinutes(5));
    }
}

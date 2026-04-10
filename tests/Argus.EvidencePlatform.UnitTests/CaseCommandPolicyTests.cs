using Argus.EvidencePlatform.Domain.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CaseCommandPolicyTests
{
    [Fact]
    public void CreateDefault_should_use_default_stream_start_fps()
    {
        var now = new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero);
        var policy = CaseCommandPolicy.CreateDefault(
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            now);

        policy.StreamStartFps.Should().Be(2);
        policy.CreatedAt.Should().Be(now);
        policy.UpdatedAt.Should().Be(now);
    }
}

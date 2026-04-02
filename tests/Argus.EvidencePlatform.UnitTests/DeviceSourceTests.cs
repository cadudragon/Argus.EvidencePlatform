using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class DeviceSourceTests
{
    [Fact]
    public void Register_should_normalize_device_and_case_ids()
    {
        var enrolledAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var result = DeviceSource.Register(
            Guid.NewGuid(),
            "  android-0123456789abcdef  ",
            Guid.NewGuid(),
            "  CASE-2026-302  ",
            enrolledAt,
            enrolledAt.AddHours(1));

        result.DeviceId.Should().Be("android-0123456789abcdef");
        result.CaseExternalId.Should().Be("CASE-2026-302");
        result.IsActive(enrolledAt.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void RenewEnrollment_should_update_case_and_validity()
    {
        var enrolledAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var result = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-302",
            enrolledAt,
            enrolledAt.AddHours(1));
        var newCaseId = Guid.NewGuid();

        result.RenewEnrollment(newCaseId, "CASE-2026-303", enrolledAt.AddMinutes(5), enrolledAt.AddHours(2));

        result.CaseId.Should().Be(newCaseId);
        result.CaseExternalId.Should().Be("CASE-2026-303");
        result.ValidUntil.Should().Be(enrolledAt.AddHours(2));
    }

    [Fact]
    public void RecordPong_should_update_last_seen_at()
    {
        var enrolledAt = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var result = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-302",
            enrolledAt,
            enrolledAt.AddHours(1));

        result.RecordPong(enrolledAt.AddMinutes(10));

        result.LastSeenAt.Should().Be(enrolledAt.AddMinutes(10));
    }
}

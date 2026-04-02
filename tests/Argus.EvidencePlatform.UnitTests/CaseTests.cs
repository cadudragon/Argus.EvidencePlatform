using Argus.EvidencePlatform.Domain.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CaseTests
{
    [Fact]
    public void Create_should_normalize_and_activate_case()
    {
        var createdAt = new DateTimeOffset(2026, 4, 1, 10, 30, 0, TimeSpan.Zero);

        var result = Case.Create(
            Guid.NewGuid(),
            "  CASE-2026-001  ",
            "  Investigation  ",
            "  High priority  ",
            createdAt);

        result.ExternalCaseId.Should().Be("CASE-2026-001");
        result.Title.Should().Be("Investigation");
        result.Description.Should().Be("High priority");
        result.Status.Should().Be(CaseStatus.Active);
        result.CreatedAt.Should().Be(createdAt);
        result.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Create_should_convert_whitespace_description_to_null()
    {
        var result = Case.Create(
            Guid.NewGuid(),
            "CASE-2026-001",
            "Investigation",
            "   ",
            DateTimeOffset.UtcNow);

        result.Description.Should().BeNull();
    }

    [Fact]
    public void Create_should_reject_blank_external_case_id()
    {
        var action = () => Case.Create(
            Guid.NewGuid(),
            "   ",
            "Investigation",
            null,
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("externalCaseId");
    }

    [Fact]
    public void Create_should_reject_blank_title()
    {
        var action = () => Case.Create(
            Guid.NewGuid(),
            "CASE-2026-001",
            "   ",
            null,
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("title");
    }
}

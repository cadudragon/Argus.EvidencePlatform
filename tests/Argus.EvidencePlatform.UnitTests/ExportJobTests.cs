using Argus.EvidencePlatform.Domain.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class ExportJobTests
{
    [Fact]
    public void Queue_should_normalize_and_mark_job_as_queued()
    {
        var requestedAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

        var result = ExportJob.Queue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Local Operator  ",
            requestedAt);

        result.RequestedBy.Should().Be("Local Operator");
        result.Status.Should().Be(ExportJobStatus.Queued);
        result.RequestedAt.Should().Be(requestedAt);
        result.CompletedAt.Should().BeNull();
        result.ManifestBlobName.Should().BeNull();
        result.PackageBlobName.Should().BeNull();
    }

    [Fact]
    public void Queue_should_reject_empty_case_id()
    {
        var action = () => ExportJob.Queue(
            Guid.NewGuid(),
            Guid.Empty,
            "Local Operator",
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("caseId");
    }

    [Fact]
    public void Queue_should_reject_empty_id()
    {
        var action = () => ExportJob.Queue(
            Guid.Empty,
            Guid.NewGuid(),
            "Local Operator",
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Fact]
    public void Queue_should_reject_blank_requested_by()
    {
        var action = () => ExportJob.Queue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ",
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("requestedBy");
    }

    [Fact]
    public void Queue_should_reject_default_requested_at()
    {
        var action = () => ExportJob.Queue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Local Operator",
            default);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("requestedAt");
    }
}

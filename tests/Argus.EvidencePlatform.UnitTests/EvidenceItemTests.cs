using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class EvidenceItemTests
{
    [Fact]
    public void Preserve_should_normalize_and_mark_as_preserved()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            "version-1",
            "image/jpeg",
            128,
            new string('a', 64),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));

        var result = EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "  device-01  ",
            EvidenceType.Image,
            new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            "  secret  ",
            blob);

        result.SourceId.Should().Be("device-01");
        result.Classification.Should().Be("secret");
        result.Status.Should().Be(EvidenceStatus.Preserved);
        result.Blob.Should().BeSameAs(blob);
    }

    [Fact]
    public void Preserve_should_convert_blank_classification_to_null()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var result = EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "device-01",
            EvidenceType.Image,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            "   ",
            blob);

        result.Classification.Should().BeNull();
    }

    [Fact]
    public void Preserve_should_reject_blank_source_id()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "   ",
            EvidenceType.Image,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            null,
            blob);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("sourceId");
    }

    [Fact]
    public void Preserve_should_reject_blob_with_different_evidence_item_id()
    {
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "device-01",
            EvidenceType.Image,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            null,
            blob);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("blob");
    }

    [Fact]
    public void Preserve_should_reject_empty_case_id()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            evidenceId,
            Guid.Empty,
            "device-01",
            EvidenceType.Image,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            null,
            blob);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("caseId");
    }

    [Fact]
    public void Preserve_should_reject_default_capture_timestamp()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "device-01",
            EvidenceType.Image,
            default,
            DateTimeOffset.UtcNow,
            null,
            blob);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("captureTimestamp");
    }

    [Fact]
    public void Preserve_should_reject_default_received_at()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "device-01",
            EvidenceType.Image,
            DateTimeOffset.UtcNow,
            default,
            null,
            blob);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("receivedAt");
    }

    [Fact]
    public void Preserve_should_reject_invalid_evidence_type()
    {
        var evidenceId = Guid.NewGuid();
        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        var action = () => EvidenceItem.Preserve(
            evidenceId,
            Guid.NewGuid(),
            "device-01",
            (EvidenceType)999,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            null,
            blob);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("evidenceType");
    }
}

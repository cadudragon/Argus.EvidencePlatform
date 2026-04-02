using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class EvidenceBlobTests
{
    [Fact]
    public void Create_should_normalize_and_initialize_blob()
    {
        var storedAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

        var result = EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  staging  ",
            "  2026/04/01/artifact.jpg  ",
            "version-1",
            "  image/jpeg  ",
            128,
            "A3B2E636571EAA5C4E8D7D8F1B7D8A9F3DDBA9FD5A2DA1B8F50C2354D7D10A2E",
            storedAt);

        result.ContainerName.Should().Be("staging");
        result.BlobName.Should().Be("2026/04/01/artifact.jpg");
        result.ContentType.Should().Be("image/jpeg");
        result.SizeBytes.Should().Be(128);
        result.Sha256.Should().Be("a3b2e636571eaa5c4e8d7d8f1b7d8a9f3ddba9fd5a2da1b8f50c2354d7d10a2e");
        result.StoredAt.Should().Be(storedAt);
        result.ImmutabilityState.Should().Be("pending");
        result.LegalHoldState.Should().Be("none");
    }

    [Fact]
    public void Create_should_reject_non_positive_size()
    {
        var action = () => EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            0,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("sizeBytes");
    }

    [Fact]
    public void Create_should_reject_invalid_sha256()
    {
        var action = () => EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            "not-a-sha",
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("sha256");
    }

    [Fact]
    public void Create_should_reject_blank_container_name()
    {
        var action = () => EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   ",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("containerName");
    }

    [Fact]
    public void Create_should_reject_empty_evidence_item_id()
    {
        var action = () => EvidenceBlob.Create(
            Guid.NewGuid(),
            Guid.Empty,
            "staging",
            "artifact.jpg",
            null,
            "image/jpeg",
            128,
            new string('a', 64),
            DateTimeOffset.UtcNow);

        action.Should()
            .Throw<ArgumentException>()
            .WithParameterName("evidenceItemId");
    }
}

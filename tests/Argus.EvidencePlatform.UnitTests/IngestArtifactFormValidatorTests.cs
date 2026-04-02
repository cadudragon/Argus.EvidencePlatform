using System.Text;
using Argus.EvidencePlatform.Api.Features.Evidence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IngestArtifactFormValidatorTests
{
    private readonly IngestArtifactFormValidator _validator = new();

    [Fact]
    public void Should_accept_valid_form()
    {
        var form = CreateValidForm();

        var result = _validator.Validate(form);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_empty_case_id()
    {
        var form = CreateValidForm(caseId: Guid.Empty);

        var result = _validator.Validate(form);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(IngestArtifactForm.CaseId));
    }

    [Fact]
    public void Should_reject_blank_source_id()
    {
        var form = CreateValidForm(sourceId: "   ");

        var result = _validator.Validate(form);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(IngestArtifactForm.SourceId));
    }

    [Fact]
    public void Should_reject_invalid_evidence_type()
    {
        var form = CreateValidForm(evidenceType: "Audio");

        var result = _validator.Validate(form);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(IngestArtifactForm.EvidenceType));
    }

    [Fact]
    public void Should_reject_missing_file()
    {
        var form = CreateValidForm(includeFile: false);

        var result = _validator.Validate(form);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(IngestArtifactForm.File));
    }

    [Fact]
    public void Should_reject_empty_file()
    {
        var form = CreateValidForm(file: CreateFile(Array.Empty<byte>()));

        var result = _validator.Validate(form);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(IngestArtifactForm.File));
    }

    private static IngestArtifactForm CreateValidForm(
        Guid? caseId = null,
        string sourceId = "device-01",
        string evidenceType = "Text",
        bool includeFile = true,
        IFormFile? file = null)
    {
        return new IngestArtifactForm
        {
            CaseId = caseId ?? Guid.NewGuid(),
            SourceId = sourceId,
            EvidenceType = evidenceType,
            CaptureTimestamp = new DateTimeOffset(2026, 4, 1, 11, 45, 0, TimeSpan.Zero),
            Classification = "secret",
            File = includeFile ? file ?? CreateFile() : null
        };
    }

    private static IFormFile CreateFile(byte[]? bytes = null)
    {
        bytes ??= Encoding.UTF8.GetBytes("artifact");
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "artifact.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
    }
}

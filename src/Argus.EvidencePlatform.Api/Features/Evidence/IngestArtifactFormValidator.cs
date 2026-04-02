using Argus.EvidencePlatform.Domain.Evidence;
using FluentValidation;

namespace Argus.EvidencePlatform.Api.Features.Evidence;

public sealed class IngestArtifactFormValidator : AbstractValidator<IngestArtifactForm>
{
    public IngestArtifactFormValidator()
    {
        RuleFor(x => x.CaseId)
            .NotEmpty();

        RuleFor(x => x.SourceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.EvidenceType)
            .NotEmpty()
            .Must(BeValidEvidenceType)
            .WithMessage("Evidence type must be one of: Image, Document, Text, Binary.");

        RuleFor(x => x.Classification)
            .MaximumLength(128);

        RuleFor(x => x.File)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(file => file!.Length > 0)
            .WithMessage("A file upload is required.");
    }

    private static bool BeValidEvidenceType(string evidenceType)
    {
        return Enum.TryParse<EvidenceType>(evidenceType, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed);
    }
}

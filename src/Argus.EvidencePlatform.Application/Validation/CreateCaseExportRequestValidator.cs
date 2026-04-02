using Argus.EvidencePlatform.Contracts.Exports;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class CreateCaseExportRequestValidator : AbstractValidator<CreateCaseExportRequest>
{
    public CreateCaseExportRequestValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Format)
            .MaximumLength(32)
            .Must(BeSupportedFormat)
            .When(x => !string.IsNullOrWhiteSpace(x.Format))
            .WithMessage("Format must be 'zip' when specified.");
        RuleFor(x => x.Reason).MaximumLength(512);
    }

    private static bool BeSupportedFormat(string format)
    {
        return string.Equals(format.Trim(), "zip", StringComparison.OrdinalIgnoreCase);
    }
}

using Argus.EvidencePlatform.Contracts.TextCaptures;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class IngestTextCaptureRequestValidator : AbstractValidator<IngestTextCaptureRequest>
{
    public IngestTextCaptureRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.CaseId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Sha256)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.CaptureTimestamp)
            .GreaterThan(0);

        RuleFor(x => x.Captures)
            .NotEmpty()
            .Must(captures => captures.Count <= 512)
            .WithMessage("A maximum of 512 captures is allowed.");

        RuleForEach(x => x.Captures).ChildRules(capture =>
        {
            capture.RuleFor(x => x.PackageName)
                .NotEmpty()
                .MaximumLength(256);

            capture.RuleFor(x => x.ClassName)
                .NotEmpty()
                .MaximumLength(256);

            capture.RuleFor(x => x.Text)
                .MaximumLength(4096);

            capture.RuleFor(x => x.ContentDescription)
                .MaximumLength(4096);
        });
    }
}

using Argus.EvidencePlatform.Contracts.Notifications;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class IngestNotificationRequestValidator : AbstractValidator<IngestNotificationRequest>
{
    public IngestNotificationRequestValidator()
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

        RuleFor(x => x.PackageName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Title)
            .MaximumLength(512);

        RuleFor(x => x.Text)
            .MaximumLength(4096);

        RuleFor(x => x.BigText)
            .MaximumLength(16384);

        RuleFor(x => x.Timestamp)
            .GreaterThan(0);

        RuleFor(x => x.Category)
            .MaximumLength(128);
    }
}

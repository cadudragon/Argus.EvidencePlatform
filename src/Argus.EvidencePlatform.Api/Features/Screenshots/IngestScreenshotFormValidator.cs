using FluentValidation;

namespace Argus.EvidencePlatform.Api.Features.Screenshots;

public sealed class IngestScreenshotFormValidator : AbstractValidator<IngestScreenshotForm>
{
    public IngestScreenshotFormValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.CaseId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Sha256)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(64)
            .Must(value => value.All(Uri.IsHexDigit))
            .WithMessage("sha256 must be a valid SHA-256 hex string.");

        RuleFor(x => x.CaptureTimestamp)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(BeValidCaptureTimestamp)
            .WithMessage("captureTimestamp must be an ISO-8601 date or Unix time in milliseconds.");

        RuleFor(x => x.Image)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(file => file!.Length > 0)
            .WithMessage("An image upload is required.")
            .Must(file => file is not null && IsSupportedImageContentType(file.ContentType))
            .WithMessage("image must be an image/jpeg, image/png, or image/webp upload.");
    }

    private static bool IsSupportedImageContentType(string? contentType)
    {
        return contentType is "image/jpeg" or "image/png" or "image/webp";
    }

    private static bool BeValidCaptureTimestamp(string value)
    {
        if (long.TryParse(value, out _))
        {
            return true;
        }

        return DateTimeOffset.TryParse(value, out _);
    }
}

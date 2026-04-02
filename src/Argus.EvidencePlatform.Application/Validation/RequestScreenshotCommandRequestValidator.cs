using Argus.EvidencePlatform.Contracts.Device;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class RequestScreenshotCommandRequestValidator : AbstractValidator<RequestScreenshotCommandRequest>
{
    public RequestScreenshotCommandRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);
    }
}

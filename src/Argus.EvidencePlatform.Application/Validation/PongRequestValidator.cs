using Argus.EvidencePlatform.Contracts.Device;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class PongRequestValidator : AbstractValidator<PongRequest>
{
    public PongRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);
    }
}

using Argus.EvidencePlatform.Contracts.Device;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class UpdateFcmTokenRequestValidator : AbstractValidator<UpdateFcmTokenRequest>
{
    public UpdateFcmTokenRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.FcmToken)
            .NotEmpty()
            .MaximumLength(4096);
    }
}

using Argus.EvidencePlatform.Contracts.Enrollment;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class ActivationRequestValidator : AbstractValidator<ActivationRequest>
{
    public ActivationRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .Matches("^[0-9]{9}$");

        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);
    }
}

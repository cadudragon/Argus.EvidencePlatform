using Argus.EvidencePlatform.Contracts.Cases;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(x => x.ExternalCaseId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2048);
    }
}

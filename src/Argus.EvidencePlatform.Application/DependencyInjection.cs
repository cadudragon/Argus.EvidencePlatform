using Argus.EvidencePlatform.Application.Validation;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Device;
using Argus.EvidencePlatform.Contracts.Enrollment;
using Argus.EvidencePlatform.Contracts.Exports;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Argus.EvidencePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateCaseRequest>, CreateCaseRequestValidator>();
        services.AddScoped<IValidator<CreateCaseExportRequest>, CreateCaseExportRequestValidator>();
        services.AddScoped<IValidator<ActivationRequest>, ActivationRequestValidator>();
        services.AddScoped<IValidator<PongRequest>, PongRequestValidator>();
        services.AddScoped<IValidator<UpdateFcmTokenRequest>, UpdateFcmTokenRequestValidator>();
        services.AddScoped<IValidator<RequestScreenshotCommandRequest>, RequestScreenshotCommandRequestValidator>();

        return services;
    }
}

using Argus.EvidencePlatform.Api.Features.Audit;
using Argus.EvidencePlatform.Api.Features.Cases;
using Argus.EvidencePlatform.Api.Features.Device;
using Argus.EvidencePlatform.Api.Features.Evidence;
using Argus.EvidencePlatform.Api.Features.Enrollment;
using Argus.EvidencePlatform.Api.Features.Exports;
using Argus.EvidencePlatform.Api.Features.Screenshots;

namespace Argus.EvidencePlatform.Api.Features;

public static class FeaturesEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapCaseEndpoints();
        builder.MapEvidenceEndpoints();
        builder.MapExportEndpoints();
        builder.MapAuditEndpoints();
        builder.MapEnrollmentEndpoints();
        builder.MapDeviceEndpoints();
        builder.MapScreenshotEndpoints();

        return builder;
    }
}

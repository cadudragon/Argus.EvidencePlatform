using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public interface IFcmCommandDataPayloadBuilder
{
    IReadOnlyDictionary<string, string> Build(DeviceCommandDispatchRequest request);
}

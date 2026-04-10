using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class GuidCommandNonceGenerator : ICommandNonceGenerator
{
    public string CreateNonce() => Guid.NewGuid().ToString("N");
}

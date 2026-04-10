using Argus.EvidencePlatform.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FcmCommandDataPayloadBuilder(
    IFcmCommandEnvelopeEncryptor encryptor,
    IOptions<FcmCommandEncryptionOptions> encryptionOptions,
    ILogger<FcmCommandDataPayloadBuilder> logger) : IFcmCommandDataPayloadBuilder
{
    public IReadOnlyDictionary<string, string> Build(DeviceCommandDispatchRequest request)
    {
        var options = encryptionOptions.Value;
        if (!options.Enabled && options.AllowPlaintextDebugFallback)
        {
            logger.LogWarning(
                "Plaintext FCM command fallback is enabled for device {DeviceId}; this is not the production contract.",
                request.DeviceId);
            return new Dictionary<string, string>
            {
                ["cmd"] = request.Command
            };
        }

        if (!options.Enabled)
        {
            throw new InvalidOperationException("FCM command encryption is disabled without plaintext debug fallback.");
        }

        var envelope = encryptor.Encrypt(
            new DeviceCommandEnvelopeRequest(
                request.Command,
                request.Parameters,
                request.IssuedAt,
                request.ExpiresAt,
                request.Nonce,
                request.DeviceCommandKey));

        return new Dictionary<string, string>
        {
            ["enc"] = envelope.Enc,
            ["alg"] = envelope.Alg,
            ["kid"] = envelope.Kid,
            ["dkid"] = envelope.Dkid,
            ["iv"] = envelope.Iv,
            ["ct"] = envelope.Ct
        };
    }
}

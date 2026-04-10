using System.Security.Cryptography;
using Argus.EvidencePlatform.Infrastructure.Firebase;

namespace Argus.EvidencePlatform.UnitTests;

internal sealed class FcmCommandTestKeys : IDisposable
{
    public ECDiffieHellman BackendKey { get; } = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    public ECDiffieHellman DeviceKey { get; } = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

    public string BackendPrivateKey =>
        FcmCommandEnvelopeEncryptor.EncodeBase64UrlNoPadding(BackendKey.ExportPkcs8PrivateKey());

    public string DevicePublicKey =>
        FcmCommandEnvelopeEncryptor.EncodeBase64UrlNoPadding(DeviceKey.ExportSubjectPublicKeyInfo());

    public void Dispose()
    {
        BackendKey.Dispose();
        DeviceKey.Dispose();
    }
}

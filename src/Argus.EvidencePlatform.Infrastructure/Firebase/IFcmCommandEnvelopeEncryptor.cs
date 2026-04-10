namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public interface IFcmCommandEnvelopeEncryptor
{
    EncryptedFcmCommandEnvelope Encrypt(DeviceCommandEnvelopeRequest request);
}

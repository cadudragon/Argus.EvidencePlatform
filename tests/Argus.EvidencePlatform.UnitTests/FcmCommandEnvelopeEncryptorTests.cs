using System.Security.Cryptography;
using System.Text;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Infrastructure.Firebase;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class FcmCommandEnvelopeEncryptorTests
{
    [Fact]
    public void Encrypt_should_create_envelope_that_can_be_decrypted_by_device_key()
    {
        using var keys = new FcmCommandTestKeys();
        var encryptor = BuildEncryptor(keys);

        var envelope = encryptor.Encrypt(CreateRequest(keys));
        var plaintext = Decrypt(envelope, keys);

        plaintext.Should().Contain("\"cmd\":\"screenshot\"");
        plaintext.Should().Contain("\"iat\":1775124000000");
        plaintext.Should().Contain("\"exp\":1775124060000");
        plaintext.Should().Contain("\"nonce\":\"nonce-001\"");
    }

    [Fact]
    public void Decrypt_should_fail_when_ciphertext_is_tampered()
    {
        using var keys = new FcmCommandTestKeys();
        var encryptor = BuildEncryptor(keys);
        var envelope = encryptor.Encrypt(CreateRequest(keys));
        var tamperedBytes = FcmCommandEnvelopeEncryptor.DecodeBase64UrlNoPadding(envelope.Ct);
        tamperedBytes[0] ^= 0x01;
        var tampered = envelope with { Ct = FcmCommandEnvelopeEncryptor.EncodeBase64UrlNoPadding(tamperedBytes) };

        var act = () => Decrypt(tampered, keys);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Payload_builder_should_not_include_plaintext_command_in_production_mode()
    {
        using var keys = new FcmCommandTestKeys();
        var builder = new FcmCommandDataPayloadBuilder(
            BuildEncryptor(keys),
            Options.Create(new FcmCommandEncryptionOptions
            {
                Enabled = true,
                BackendKeyId = "backend-ecdh-dev-2026-04",
                BackendPrivateKey = keys.BackendPrivateKey
            }),
            NullLogger<FcmCommandDataPayloadBuilder>.Instance);

        var payload = builder.Build(new DeviceCommandDispatchRequest(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "android-0123456789abcdef",
            "fcm-token",
            "screenshot",
            new Dictionary<string, string>(),
            new DeviceCommandKey("ECDH-P256", "device-ecdh-0123456789abcdef", keys.DevicePublicKey),
            new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 10, 1, 0, TimeSpan.Zero),
            "nonce-001"));

        payload.Keys.Should().BeEquivalentTo("enc", "alg", "kid", "dkid", "iv", "ct");
        payload.Should().NotContainKey("cmd");
        payload.Values.Should().NotContain("screenshot");
        payload.Values.Should().NotContain(value => value.Contains("\"cmd\"", StringComparison.Ordinal));
        payload["dkid"].Should().Be("device-ecdh-0123456789abcdef");
    }

    private static FcmCommandEnvelopeEncryptor BuildEncryptor(FcmCommandTestKeys keys)
    {
        return new FcmCommandEnvelopeEncryptor(
            Options.Create(new FcmCommandEncryptionOptions
            {
                Enabled = true,
                BackendKeyId = "backend-ecdh-dev-2026-04",
                BackendPrivateKey = keys.BackendPrivateKey
            }));
    }

    private static DeviceCommandEnvelopeRequest CreateRequest(FcmCommandTestKeys keys)
    {
        return new DeviceCommandEnvelopeRequest(
            "screenshot",
            new Dictionary<string, string>(),
            new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 10, 1, 0, TimeSpan.Zero),
            "nonce-001",
            new DeviceCommandKey("ECDH-P256", "device-ecdh-0123456789abcdef", keys.DevicePublicKey));
    }

    private static string Decrypt(EncryptedFcmCommandEnvelope envelope, FcmCommandTestKeys keys)
    {
        var iv = FcmCommandEnvelopeEncryptor.DecodeBase64UrlNoPadding(envelope.Iv);
        var combined = FcmCommandEnvelopeEncryptor.DecodeBase64UrlNoPadding(envelope.Ct);
        var ciphertext = combined[..^FcmCommandEnvelopeContract.TagSizeBytes];
        var tag = combined[^FcmCommandEnvelopeContract.TagSizeBytes..];
        var plaintext = new byte[ciphertext.Length];
        var aad = Encoding.UTF8.GetBytes(FcmCommandEnvelopeEncryptor.CreateCanonicalAad(envelope.Kid, envelope.Dkid, envelope.Iv));
        var key = DeriveDeviceKey(keys, envelope.Kid, envelope.Dkid);

        using var aesGcm = new AesGcm(key, FcmCommandEnvelopeContract.TagSizeBytes);
        aesGcm.Decrypt(iv, ciphertext, tag, plaintext, aad);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] DeriveDeviceKey(FcmCommandTestKeys keys, string backendKeyId, string deviceKeyId)
    {
        using var backendPublic = ECDiffieHellman.Create();
        backendPublic.ImportSubjectPublicKeyInfo(keys.BackendKey.ExportSubjectPublicKeyInfo(), out _);
        var sharedSecret = keys.DeviceKey.DeriveRawSecretAgreement(backendPublic.PublicKey);
        var salt = Encoding.UTF8.GetBytes("Argus-FCM-v1" + backendKeyId + deviceKeyId);
        var info = Encoding.UTF8.GetBytes("Argus FCM Command Envelope v1");
        return HkdfSha256(sharedSecret, salt, info, FcmCommandEnvelopeContract.KeySizeBytes);
    }

    private static byte[] HkdfSha256(byte[] inputKeyMaterial, byte[] salt, byte[] info, int outputLength)
    {
        using var extract = new HMACSHA256(salt);
        var pseudorandomKey = extract.ComputeHash(inputKeyMaterial);
        var output = new byte[outputLength];
        var previous = Array.Empty<byte>();
        var generated = 0;
        byte counter = 1;

        using var expand = new HMACSHA256(pseudorandomKey);
        while (generated < outputLength)
        {
            var blockInput = new byte[previous.Length + info.Length + 1];
            Buffer.BlockCopy(previous, 0, blockInput, 0, previous.Length);
            Buffer.BlockCopy(info, 0, blockInput, previous.Length, info.Length);
            blockInput[^1] = counter;
            previous = expand.ComputeHash(blockInput);

            var bytesToCopy = Math.Min(previous.Length, outputLength - generated);
            Buffer.BlockCopy(previous, 0, output, generated, bytesToCopy);
            generated += bytesToCopy;
            counter++;
        }

        return output;
    }
}

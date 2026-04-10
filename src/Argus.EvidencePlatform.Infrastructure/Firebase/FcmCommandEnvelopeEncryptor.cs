using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FcmCommandEnvelopeEncryptor(IOptions<FcmCommandEncryptionOptions> options)
    : IFcmCommandEnvelopeEncryptor
{
    private const string HkdfInfo = "Argus FCM Command Envelope v1";
    private const string HkdfSaltPrefix = "Argus-FCM-v1";

    public EncryptedFcmCommandEnvelope Encrypt(DeviceCommandEnvelopeRequest request)
    {
        var encryptionOptions = options.Value;
        var backendKeyId = NormalizeRequired(encryptionOptions.BackendKeyId, nameof(encryptionOptions.BackendKeyId));
        var backendPrivateKey = NormalizeRequired(encryptionOptions.BackendPrivateKey, nameof(encryptionOptions.BackendPrivateKey));
        var deviceKeyId = NormalizeRequired(request.DeviceKey.Kid, nameof(request.DeviceKey.Kid));

        if (request.DeviceKey.Alg != FcmCommandEnvelopeContract.DeviceKeyAlg)
        {
            throw new InvalidOperationException("Unsupported device command key algorithm.");
        }

        ValidateCommand(request);

        var iv = RandomNumberGenerator.GetBytes(FcmCommandEnvelopeContract.IvSizeBytes);
        var encodedIv = EncodeBase64UrlNoPadding(iv);
        var aad = Encoding.UTF8.GetBytes(CreateCanonicalAad(backendKeyId, deviceKeyId, encodedIv));
        var plaintext = Encoding.UTF8.GetBytes(CreatePlaintextJson(request));
        var key = DeriveAesKey(backendPrivateKey, request.DeviceKey.PublicKey, backendKeyId, deviceKeyId);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[FcmCommandEnvelopeContract.TagSizeBytes];

        using (var aesGcm = new AesGcm(key, FcmCommandEnvelopeContract.TagSizeBytes))
        {
            aesGcm.Encrypt(iv, plaintext, ciphertext, tag, aad);
        }

        var combined = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, combined, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, ciphertext.Length, tag.Length);

        return new EncryptedFcmCommandEnvelope(
            FcmCommandEnvelopeContract.Enc,
            FcmCommandEnvelopeContract.Alg,
            backendKeyId,
            deviceKeyId,
            encodedIv,
            EncodeBase64UrlNoPadding(combined));
    }

    public static string CreateCanonicalAad(string backendKeyId, string deviceKeyId, string iv)
    {
        return $$"""{"enc":"1","alg":"ECDH-P256-HKDF-SHA256+A256GCM","kid":"{{backendKeyId}}","dkid":"{{deviceKeyId}}","iv":"{{iv}}"}""";
    }

    public static byte[] DecodeBase64UrlNoPadding(string value)
    {
        if (value.Contains('='))
        {
            throw new FormatException("Padding is not allowed.");
        }

        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        return Convert.FromBase64String(base64);
    }

    public static string EncodeBase64UrlNoPadding(ReadOnlySpan<byte> value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] DeriveAesKey(
        string backendPrivateKey,
        string devicePublicKey,
        string backendKeyId,
        string deviceKeyId)
    {
        using var backendEcdh = ECDiffieHellman.Create();
        var privateKeyBytes = DecodeBase64UrlNoPadding(backendPrivateKey);
        backendEcdh.ImportPkcs8PrivateKey(privateKeyBytes, out var privateBytesRead);
        if (privateBytesRead != privateKeyBytes.Length)
        {
            throw new InvalidOperationException("Backend private key contains trailing data.");
        }

        using var deviceEcdh = ECDiffieHellman.Create();
        var publicKeyBytes = DecodeBase64UrlNoPadding(devicePublicKey);
        deviceEcdh.ImportSubjectPublicKeyInfo(publicKeyBytes, out var publicBytesRead);
        if (publicBytesRead != publicKeyBytes.Length)
        {
            throw new InvalidOperationException("Device public key contains trailing data.");
        }

        var sharedSecret = backendEcdh.DeriveKeyMaterial(deviceEcdh.PublicKey);
        var salt = Encoding.UTF8.GetBytes(HkdfSaltPrefix + backendKeyId + deviceKeyId);
        var info = Encoding.UTF8.GetBytes(HkdfInfo);
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

    private static string CreatePlaintextJson(DeviceCommandEnvelopeRequest request)
    {
        var issuedAt = request.IssuedAt.ToUnixTimeMilliseconds();
        var expiresAt = request.ExpiresAt.ToUnixTimeMilliseconds();

        return request.Command switch
        {
            "stream_start" => $$"""{"cmd":"stream_start","fps":"{{request.Parameters["fps"]}}","iat":{{issuedAt}},"exp":{{expiresAt}},"nonce":"{{request.Nonce}}"}""",
            "stream_stop" => $$"""{"cmd":"stream_stop","iat":{{issuedAt}},"exp":{{expiresAt}},"nonce":"{{request.Nonce}}"}""",
            "ping" => $$"""{"cmd":"ping","iat":{{issuedAt}},"exp":{{expiresAt}},"nonce":"{{request.Nonce}}"}""",
            _ => $$"""{"cmd":"screenshot","iat":{{issuedAt}},"exp":{{expiresAt}},"nonce":"{{request.Nonce}}"}"""
        };
    }

    private static void ValidateCommand(DeviceCommandEnvelopeRequest request)
    {
        if (request.ExpiresAt <= request.IssuedAt)
        {
            throw new InvalidOperationException("Command expiration must be greater than issued-at.");
        }

        if (request.ExpiresAt - request.IssuedAt > TimeSpan.FromMinutes(FcmCommandEnvelopeContract.MaxTtlMinutes))
        {
            throw new InvalidOperationException("Command expiration exceeds the maximum TTL.");
        }

        if (string.IsNullOrWhiteSpace(request.Nonce))
        {
            throw new InvalidOperationException("Command nonce is required.");
        }

        if (request.Command is "stream_start")
        {
            if (!request.Parameters.TryGetValue("fps", out var fps) || string.IsNullOrWhiteSpace(fps))
            {
                throw new InvalidOperationException("stream_start requires fps.");
            }

            return;
        }

        if (request.Command is not ("screenshot" or "stream_stop" or "ping"))
        {
            throw new InvalidOperationException("Unsupported command.");
        }
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{paramName} is required.");
        }

        return value.Trim();
    }
}

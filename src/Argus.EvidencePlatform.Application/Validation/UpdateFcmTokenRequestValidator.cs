using System.Security.Cryptography;
using Argus.EvidencePlatform.Contracts.Device;
using FluentValidation;

namespace Argus.EvidencePlatform.Application.Validation;

public sealed class UpdateFcmTokenRequestValidator : AbstractValidator<UpdateFcmTokenRequest>
{
    public UpdateFcmTokenRequestValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.FcmToken)
            .NotEmpty()
            .MaximumLength(4096);

        RuleFor(x => x.FcmCommandKey)
            .NotNull();

        When(x => x.FcmCommandKey is not null, () =>
        {
            RuleFor(x => x.FcmCommandKey!.Alg)
                .Equal("ECDH-P256");

            RuleFor(x => x.FcmCommandKey!.Kid)
                .NotEmpty()
                .MaximumLength(128);

            RuleFor(x => x.FcmCommandKey!.PublicKey)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(BeImportableP256PublicKey)
                .WithMessage("PublicKey must be a base64url-no-padding SPKI ECDH P-256 public key.");
        });
    }

    private static bool BeImportableP256PublicKey(string value)
    {
        try
        {
            var bytes = DecodeBase64UrlNoPadding(value);
            using var ecdh = ECDiffieHellman.Create();
            ecdh.ImportSubjectPublicKeyInfo(bytes, out var bytesRead);
            return bytesRead == bytes.Length && ecdh.ExportParameters(false).Curve.Oid.Value == "1.2.840.10045.3.1.7";
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64UrlNoPadding(string value)
    {
        if (value.Contains('='))
        {
            throw new FormatException("Padding is not allowed.");
        }

        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        return Convert.FromBase64String(base64);
    }
}

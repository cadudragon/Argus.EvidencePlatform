namespace Argus.EvidencePlatform.Application.Device.BindFcmToken;

public sealed record FcmCommandKeyInput(
    string Alg,
    string Kid,
    string PublicKey);

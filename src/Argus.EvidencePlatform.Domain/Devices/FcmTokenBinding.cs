namespace Argus.EvidencePlatform.Domain.Devices;

public sealed class FcmTokenBinding
{
    public Guid Id { get; private set; }
    public Guid FirebaseAppId { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string FcmToken { get; private set; } = string.Empty;
    public string FcmCommandKeyAlg { get; private set; } = string.Empty;
    public string FcmCommandKeyKid { get; private set; } = string.Empty;
    public string FcmCommandKeyPublicKey { get; private set; } = string.Empty;
    public DateTimeOffset BoundAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private FcmTokenBinding()
    {
    }

    public static FcmTokenBinding Bind(
        Guid id,
        Guid firebaseAppId,
        string deviceId,
        string fcmToken,
        string fcmCommandKeyAlg,
        string fcmCommandKeyKid,
        string fcmCommandKeyPublicKey,
        DateTimeOffset boundAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (firebaseAppId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(firebaseAppId));
        }

        if (boundAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(boundAt));
        }

        return new FcmTokenBinding
        {
            Id = id,
            FirebaseAppId = firebaseAppId,
            DeviceId = NormalizeRequired(deviceId, nameof(deviceId)),
            FcmToken = NormalizeRequired(fcmToken, nameof(fcmToken)),
            FcmCommandKeyAlg = NormalizeRequired(fcmCommandKeyAlg, nameof(fcmCommandKeyAlg)),
            FcmCommandKeyKid = NormalizeRequired(fcmCommandKeyKid, nameof(fcmCommandKeyKid)),
            FcmCommandKeyPublicKey = NormalizeRequired(fcmCommandKeyPublicKey, nameof(fcmCommandKeyPublicKey)),
            BoundAt = boundAt,
            UpdatedAt = boundAt
        };
    }

    public void UpdateToken(
        Guid firebaseAppId,
        string fcmToken,
        string fcmCommandKeyAlg,
        string fcmCommandKeyKid,
        string fcmCommandKeyPublicKey,
        DateTimeOffset updatedAt)
    {
        if (firebaseAppId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(firebaseAppId));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(updatedAt));
        }

        FirebaseAppId = firebaseAppId;
        FcmToken = NormalizeRequired(fcmToken, nameof(fcmToken));
        FcmCommandKeyAlg = NormalizeRequired(fcmCommandKeyAlg, nameof(fcmCommandKeyAlg));
        FcmCommandKeyKid = NormalizeRequired(fcmCommandKeyKid, nameof(fcmCommandKeyKid));
        FcmCommandKeyPublicKey = NormalizeRequired(fcmCommandKeyPublicKey, nameof(fcmCommandKeyPublicKey));
        UpdatedAt = updatedAt;
    }

    public bool HasCommandKey()
    {
        return !string.IsNullOrWhiteSpace(FcmCommandKeyAlg)
            && !string.IsNullOrWhiteSpace(FcmCommandKeyKid)
            && !string.IsNullOrWhiteSpace(FcmCommandKeyPublicKey);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}

namespace Argus.EvidencePlatform.Domain.Devices;

public sealed class FcmTokenBinding
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string FcmToken { get; private set; } = string.Empty;
    public DateTimeOffset BoundAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private FcmTokenBinding()
    {
    }

    public static FcmTokenBinding Bind(
        Guid id,
        string deviceId,
        string fcmToken,
        DateTimeOffset boundAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (boundAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(boundAt));
        }

        return new FcmTokenBinding
        {
            Id = id,
            DeviceId = NormalizeRequired(deviceId, nameof(deviceId)),
            FcmToken = NormalizeRequired(fcmToken, nameof(fcmToken)),
            BoundAt = boundAt,
            UpdatedAt = boundAt
        };
    }

    public void UpdateToken(string fcmToken, DateTimeOffset updatedAt)
    {
        if (updatedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(updatedAt));
        }

        FcmToken = NormalizeRequired(fcmToken, nameof(fcmToken));
        UpdatedAt = updatedAt;
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

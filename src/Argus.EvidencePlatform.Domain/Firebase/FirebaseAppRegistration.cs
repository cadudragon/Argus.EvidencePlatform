using System.Security.Cryptography;
using System.Text;

namespace Argus.EvidencePlatform.Domain.Firebase;

public sealed class FirebaseAppRegistration
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string ProjectId { get; private set; } = string.Empty;
    public string ServiceAccountPath { get; private set; } = string.Empty;
    public bool IsActiveForNewCases { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private FirebaseAppRegistration()
    {
    }

    public static FirebaseAppRegistration Create(
        string key,
        string displayName,
        string projectId,
        string serviceAccountPath,
        bool isActiveForNewCases,
        DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(createdAt));
        }

        var normalizedKey = NormalizeRequired(key, nameof(key));

        return new FirebaseAppRegistration
        {
            Id = CreateDeterministicId(normalizedKey),
            Key = normalizedKey,
            DisplayName = NormalizeRequired(displayName, nameof(displayName)),
            ProjectId = NormalizeRequired(projectId, nameof(projectId)),
            ServiceAccountPath = NormalizeRequired(serviceAccountPath, nameof(serviceAccountPath)),
            IsActiveForNewCases = isActiveForNewCases,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateConfiguration(
        string displayName,
        string projectId,
        string serviceAccountPath,
        bool isActiveForNewCases,
        DateTimeOffset updatedAt)
    {
        if (updatedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(updatedAt));
        }

        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        ProjectId = NormalizeRequired(projectId, nameof(projectId));
        ServiceAccountPath = NormalizeRequired(serviceAccountPath, nameof(serviceAccountPath));
        IsActiveForNewCases = isActiveForNewCases;
        UpdatedAt = updatedAt;
    }

    public static Guid CreateDeterministicId(string key)
    {
        var normalizedKey = NormalizeRequired(key, nameof(key));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedKey));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
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

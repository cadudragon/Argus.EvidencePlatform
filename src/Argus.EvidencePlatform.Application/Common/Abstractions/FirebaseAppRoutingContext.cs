namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record FirebaseAppRoutingContext(
    Guid FirebaseAppId,
    string Key,
    string ProjectId);

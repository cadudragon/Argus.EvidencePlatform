namespace Argus.EvidencePlatform.Api.Authentication;

public sealed class PlatformAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Mode { get; set; } = AuthenticationModes.Jwt;

    public string? Authority { get; set; }

    public string? Audience { get; set; }

    public MockAuthenticationOptions Mock { get; set; } = new();
}

public static class AuthenticationModes
{
    public const string Jwt = "Jwt";
    public const string Mock = "Mock";
}


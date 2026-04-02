namespace Argus.EvidencePlatform.Api.Authentication;

public sealed class MockAuthenticationOptions
{
    public string SubjectId { get; set; } = "local-operator";

    public string Name { get; set; } = "Local Operator";

    public string[] Roles { get; set; } = ["Operator"];
}


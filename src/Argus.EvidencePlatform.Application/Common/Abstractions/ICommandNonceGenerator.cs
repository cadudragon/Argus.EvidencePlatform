namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface ICommandNonceGenerator
{
    string CreateNonce();
}

using Argus.EvidencePlatform.Domain.Firebase;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IFirebaseAppRepository
{
    Task<FirebaseAppRegistration?> GetByIdAsync(Guid firebaseAppId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FirebaseAppRegistration>> ListActiveForNewCasesAsync(CancellationToken cancellationToken);
}

using Argus.EvidencePlatform.Domain.TextCaptures;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface ITextCaptureBatchRepository
{
    Task AddAsync(TextCaptureBatch entity, CancellationToken cancellationToken);
}

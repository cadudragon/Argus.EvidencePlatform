using Argus.EvidencePlatform.Contracts.Exports;
using Argus.EvidencePlatform.Domain.Exports;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IExportJobRepository
{
    Task AddAsync(ExportJob entity, CancellationToken cancellationToken);
    Task<ExportJobResponse?> GetByIdAsync(Guid exportJobId, CancellationToken cancellationToken);
}

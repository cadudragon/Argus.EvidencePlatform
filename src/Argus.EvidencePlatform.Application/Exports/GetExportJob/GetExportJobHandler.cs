using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Exports;

namespace Argus.EvidencePlatform.Application.Exports.GetExportJob;

public sealed class GetExportJobHandler(IExportJobRepository exportJobRepository)
{
    public Task<ExportJobResponse?> Handle(GetExportJobQuery query, CancellationToken cancellationToken)
    {
        return exportJobRepository.GetByIdAsync(query.ExportJobId, cancellationToken);
    }
}

using Argus.EvidencePlatform.Contracts.Exports;

namespace Argus.EvidencePlatform.Application.Exports.CreateCaseExport;

public sealed record CreateCaseExportCommand(
    Guid CaseId,
    string RequestedBy,
    string? Format,
    string? Reason);

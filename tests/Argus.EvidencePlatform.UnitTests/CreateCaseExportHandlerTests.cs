using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Exports.CreateCaseExport;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Exports;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Cases;
using Argus.EvidencePlatform.Domain.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CreateCaseExportHandlerTests
{
    [Fact]
    public async Task Handle_should_return_null_when_case_does_not_exist()
    {
        var caseRepository = new FakeCaseRepository();
        var exportJobRepository = new FakeExportJobRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCaseExportHandler(
            caseRepository,
            exportJobRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new CreateCaseExportCommand(Guid.NewGuid(), "Local Operator", "zip", "Need export"),
            CancellationToken.None);

        result.Should().BeNull();
        exportJobRepository.AddedJobs.Should().BeEmpty();
        auditRepository.AddedEntries.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_should_queue_export_and_write_audit_entry()
    {
        var caseId = Guid.NewGuid();
        var caseRepository = new FakeCaseRepository
        {
            ExistsResult = true,
            Response = new CaseResponse(
                caseId,
                "CASE-2026-201",
                "Export",
                null,
                nameof(CaseStatus.Active),
                new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                null)
        };
        var exportJobRepository = new FakeExportJobRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));
        var handler = new CreateCaseExportHandler(
            caseRepository,
            exportJobRepository,
            auditRepository,
            clock,
            unitOfWork);

        var result = await handler.Handle(
            new CreateCaseExportCommand(caseId, "  Local Operator  ", " ZIP ", "  Need export  "),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.CaseId.Should().Be(caseId);
        result.Status.Should().Be(nameof(ExportJobStatus.Queued));
        result.RequestedBy.Should().Be("Local Operator");
        result.RequestedAt.Should().Be(clock.UtcNow);
        exportJobRepository.AddedJobs.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var job = exportJobRepository.AddedJobs.Single();
        job.CaseId.Should().Be(caseId);
        job.RequestedBy.Should().Be("Local Operator");
        job.Status.Should().Be(ExportJobStatus.Queued);
        auditRepository.AddedEntries.Should().ContainSingle();

        var auditEntry = auditRepository.AddedEntries.Single();
        auditEntry.CaseId.Should().Be(caseId);
        auditEntry.ActorType.Should().Be(AuditActorType.Operator);
        auditEntry.ActorId.Should().Be("  Local Operator  ");
        auditEntry.Action.Should().Be("ExportQueued");
        auditEntry.EntityType.Should().Be(nameof(ExportJob));
        auditEntry.EntityId.Should().Be(job.Id);

        using var payload = JsonDocument.Parse(auditEntry.PayloadJson!);
        payload.RootElement.GetProperty("Format").GetString().Should().Be("zip");
        payload.RootElement.GetProperty("Reason").GetString().Should().Be("Need export");
    }

    [Fact]
    public async Task Handle_should_normalize_optional_audit_fields_to_null()
    {
        var caseId = Guid.NewGuid();
        var caseRepository = new FakeCaseRepository
        {
            ExistsResult = true,
            Response = new CaseResponse(
                caseId,
                "CASE-2026-202",
                "Export",
                null,
                nameof(CaseStatus.Active),
                new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                null)
        };
        var exportJobRepository = new FakeExportJobRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCaseExportHandler(
            caseRepository,
            exportJobRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new CreateCaseExportCommand(caseId, "Local Operator", null, "   "),
            CancellationToken.None);

        result.Should().NotBeNull();
        using var payload = JsonDocument.Parse(auditRepository.AddedEntries.Single().PayloadJson!);
        payload.RootElement.GetProperty("Format").ValueKind.Should().Be(JsonValueKind.Null);
        payload.RootElement.GetProperty("Reason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public bool ExistsResult { get; set; }
        public CaseResponse? Response { get; set; }

        public Task AddAsync(Case entity, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistsResult);
        }

        public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Response);
        }
    }

    private sealed class FakeExportJobRepository : IExportJobRepository
    {
        public List<ExportJob> AddedJobs { get; } = [];

        public Task AddAsync(ExportJob entity, CancellationToken cancellationToken)
        {
            AddedJobs.Add(entity);
            return Task.CompletedTask;
        }

        public Task<ExportJobResponse?> GetByIdAsync(Guid exportJobId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public List<AuditEntry> AddedEntries { get; } = [];

        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
        {
            AddedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEntryResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}

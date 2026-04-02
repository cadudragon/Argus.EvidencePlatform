using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.IngestArtifact;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Cases;
using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class FinalizeEvidenceIntakeHandlerTests
{
    [Fact]
    public async Task Handle_should_return_null_when_case_does_not_exist()
    {
        var caseRepository = new FakeCaseRepository();
        var evidenceRepository = new FakeEvidenceRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new FinalizeEvidenceIntakeHandler(
            caseRepository,
            evidenceRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().BeNull();
        evidenceRepository.AddedEvidence.Should().BeEmpty();
        auditRepository.AddedEntries.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_should_preserve_evidence_and_write_audit_entry()
    {
        var caseId = Guid.NewGuid();
        var caseRepository = new FakeCaseRepository
        {
            ExistsResult = true,
            Response = new CaseResponse(
                caseId,
                "CASE-2026-001",
                "Investigation",
                null,
                nameof(CaseStatus.Active),
                new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                null)
        };
        var evidenceRepository = new FakeEvidenceRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));
        var handler = new FinalizeEvidenceIntakeHandler(
            caseRepository,
            evidenceRepository,
            auditRepository,
            clock,
            unitOfWork);

        var result = await handler.Handle(CreateCommand(caseId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CaseId.Should().Be(caseId);
        result.SourceId.Should().Be("device-01");
        result.EvidenceType.Should().Be(nameof(EvidenceType.Image));
        result.Sha256.Should().Be(new string('a', 64));
        result.SizeBytes.Should().Be(128);
        result.Status.Should().Be(nameof(EvidenceStatus.Preserved));
        result.ReceivedAt.Should().Be(clock.UtcNow);
        evidenceRepository.AddedEvidence.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var evidence = evidenceRepository.AddedEvidence.Single();
        evidence.CaseId.Should().Be(caseId);
        evidence.SourceId.Should().Be("device-01");
        evidence.Classification.Should().Be("secret");
        evidence.EvidenceType.Should().Be(EvidenceType.Image);
        evidence.Blob.ContainerName.Should().Be("staging");
        evidence.Blob.BlobName.Should().Be("2026/04/01/artifact.jpg");
        auditRepository.AddedEntries.Should().ContainSingle();

        var auditEntry = auditRepository.AddedEntries.Single();
        auditEntry.CaseId.Should().Be(caseId);
        auditEntry.Action.Should().Be("EvidencePreserved");
        auditEntry.EntityType.Should().Be(nameof(EvidenceItem));
        auditEntry.ActorType.Should().Be(AuditActorType.System);
        auditEntry.PayloadJson.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditEntry.PayloadJson!);
        payload.RootElement.GetProperty("SourceId").GetString().Should().Be("device-01");
        payload.RootElement.GetProperty("EvidenceType").GetString().Should().Be(nameof(EvidenceType.Image));
        payload.RootElement.GetProperty("BlobName").GetString().Should().Be("2026/04/01/artifact.jpg");
        payload.RootElement.GetProperty("Sha256").GetString().Should().Be(new string('a', 64));
    }

    private static FinalizeEvidenceIntakeCommand CreateCommand(Guid? caseId = null)
    {
        return new FinalizeEvidenceIntakeCommand(
            caseId ?? Guid.NewGuid(),
            "  device-01  ",
            EvidenceType.Image,
            new DateTimeOffset(2026, 4, 1, 11, 45, 0, TimeSpan.Zero),
            "  secret  ",
            new StagedBlobDescriptor(
                "staging",
                "2026/04/01/artifact.jpg",
                "image/jpeg",
                128,
                new string('a', 64),
                "version-1"));
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public bool ExistsResult { get; set; }

        public CaseResponse? Response { get; set; }

        public Task AddAsync(Case entity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

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

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public List<EvidenceItem> AddedEvidence { get; } = [];

        public Task AddAsync(EvidenceItem entity, CancellationToken cancellationToken)
        {
            AddedEvidence.Add(entity);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EvidenceTimelineItemResponse>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken)
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

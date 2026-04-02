using System.Text.Json;
using Argus.EvidencePlatform.Application.Cases.CreateCase;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class CreateCaseHandlerTests
{
    [Fact]
    public async Task Handle_should_create_case_and_audit_entry()
    {
        var caseRepository = new FakeCaseRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));
        var handler = new CreateCaseHandler(caseRepository, auditRepository, clock, unitOfWork);

        var result = await handler.Handle(
            new CreateCaseCommand("  CASE-2026-001  ", "  Investigation  ", "   High priority   "),
            CancellationToken.None);

        result.AlreadyExists.Should().BeFalse();
        result.Case.Should().NotBeNull();
        result.Case!.ExternalCaseId.Should().Be("CASE-2026-001");
        result.Case.Title.Should().Be("Investigation");
        result.Case.Description.Should().Be("High priority");
        result.Case.Status.Should().Be(nameof(CaseStatus.Active));
        result.Case.CreatedAt.Should().Be(clock.UtcNow);
        caseRepository.LastExternalCaseIdLookup.Should().Be("CASE-2026-001");
        caseRepository.AddedCases.Should().ContainSingle();
        auditRepository.AddedEntries.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var auditEntry = auditRepository.AddedEntries.Single();
        auditEntry.CaseId.Should().Be(result.Case.Id);
        auditEntry.ActorType.Should().Be(AuditActorType.System);
        auditEntry.ActorId.Should().Be("system");
        auditEntry.Action.Should().Be("CaseCreated");
        auditEntry.EntityType.Should().Be(nameof(Case));
        auditEntry.EntityId.Should().Be(result.Case.Id);
        auditEntry.OccurredAt.Should().Be(clock.UtcNow);
        auditEntry.PayloadJson.Should().NotBeNull();

        using var payload = JsonDocument.Parse(auditEntry.PayloadJson!);
        payload.RootElement.GetProperty("ExternalCaseId").GetString().Should().Be("CASE-2026-001");
        payload.RootElement.GetProperty("Title").GetString().Should().Be("Investigation");
    }

    [Fact]
    public async Task Handle_should_return_conflict_when_external_case_id_already_exists()
    {
        var caseRepository = new FakeCaseRepository
        {
            ExistsByExternalCaseIdResult = true
        };
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCaseHandler(
            caseRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new CreateCaseCommand("  CASE-2026-001  ", "Investigation", null),
            CancellationToken.None);

        result.AlreadyExists.Should().BeTrue();
        result.Case.Should().BeNull();
        caseRepository.LastExternalCaseIdLookup.Should().Be("CASE-2026-001");
        caseRepository.AddedCases.Should().BeEmpty();
        auditRepository.AddedEntries.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_should_reject_blank_external_case_id()
    {
        var caseRepository = new FakeCaseRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCaseHandler(
            caseRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var action = () => handler.Handle(
            new CreateCaseCommand("   ", "Investigation", null),
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("externalCaseId");

        caseRepository.LastExternalCaseIdLookup.Should().BeNull();
        caseRepository.AddedCases.Should().BeEmpty();
        auditRepository.AddedEntries.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_should_reject_null_external_case_id()
    {
        var caseRepository = new FakeCaseRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateCaseHandler(
            caseRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var action = () => handler.Handle(
            new CreateCaseCommand(null!, "Investigation", null),
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("externalCaseId");

        caseRepository.LastExternalCaseIdLookup.Should().BeNull();
        caseRepository.AddedCases.Should().BeEmpty();
        auditRepository.AddedEntries.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public List<Case> AddedCases { get; } = [];

        public bool ExistsByExternalCaseIdResult { get; set; }

        public string? LastExternalCaseIdLookup { get; private set; }

        public Task AddAsync(Case entity, CancellationToken cancellationToken)
        {
            AddedCases.Add(entity);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedCases.Any(x => x.Id == caseId));
        }

        public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            LastExternalCaseIdLookup = externalCaseId;
            return Task.FromResult(ExistsByExternalCaseIdResult);
        }

        public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(AddedCases
                .Where(x => x.ExternalCaseId == externalCaseId)
                .Select(x => x.Id)
                .SingleOrDefault());
        }

        public Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken)
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

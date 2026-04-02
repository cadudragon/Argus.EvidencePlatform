using Argus.EvidencePlatform.Application.Audit.GetCaseAuditTrail;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class GetCaseAuditTrailHandlerTests
{
    [Fact]
    public async Task Handle_should_return_audit_entries_when_repository_finds_them()
    {
        var caseId = Guid.NewGuid();
        var expected = new List<AuditEntryResponse>
        {
            new(
                Guid.NewGuid(),
                caseId,
                nameof(AuditActorType.System),
                "system",
                "CaseCreated",
                "Case",
                Guid.NewGuid(),
                new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero),
                "corr-1",
                "{\"externalCaseId\":\"CASE-2026-001\"}")
        };
        var repository = new FakeAuditRepository(expected);
        var handler = new GetCaseAuditTrailHandler(repository);
        var query = new GetCaseAuditTrailQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(expected);
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task Handle_should_return_empty_list_when_repository_has_no_entries()
    {
        var caseId = Guid.NewGuid();
        var repository = new FakeAuditRepository([]);
        var handler = new GetCaseAuditTrailHandler(repository);
        var query = new GetCaseAuditTrailQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    private sealed class FakeAuditRepository(IReadOnlyList<AuditEntryResponse> response) : IAuditRepository
    {
        public Guid? LastRequestedCaseId { get; private set; }

        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<AuditEntryResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        {
            LastRequestedCaseId = caseId;
            return Task.FromResult(response);
        }
    }
}

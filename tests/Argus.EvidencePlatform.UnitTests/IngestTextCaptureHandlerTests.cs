using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.TextCaptures.IngestTextCapture;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Domain.TextCaptures;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IngestTextCaptureHandlerTests
{
    [Fact]
    public async Task Handle_should_return_conflict_when_device_belongs_to_other_case()
    {
        var expectedCaseId = Guid.NewGuid();
        var caseRepository = new FakeCaseRepository { ExistingCaseId = expectedCaseId };
        var deviceRepository = new FakeDeviceSourceRepository
        {
            ExistingSource = DeviceSource.Register(
                Guid.NewGuid(),
                "android-0123456789abcdef",
                Guid.NewGuid(),
                "CASE-OTHER-002",
                new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero))
        };
        var handler = new IngestTextCaptureHandler(
            caseRepository,
            deviceRepository,
            new FakeTextCaptureBatchRepository(),
            new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new IngestTextCaptureCommand(
                "android-0123456789abcdef",
                "CASE-2026-910",
                "sha",
                DateTimeOffset.FromUnixTimeMilliseconds(1775156400000),
                [
                    new TextCapturePayload("com.whatsapp", "android.widget.TextView", "Message content", null)
                ]),
            CancellationToken.None);

        result.Should().Be(IngestTextCaptureOutcome.Conflict);
    }

    [Fact]
    public async Task Handle_should_persist_batch_and_write_audit_entry()
    {
        var caseId = Guid.NewGuid();
        var batchRepository = new FakeTextCaptureBatchRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new IngestTextCaptureHandler(
            new FakeCaseRepository { ExistingCaseId = caseId },
            new FakeDeviceSourceRepository
            {
                ExistingSource = DeviceSource.Register(
                    Guid.NewGuid(),
                    "android-0123456789abcdef",
                    caseId,
                    "CASE-2026-910",
                    new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero))
            },
            batchRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new IngestTextCaptureCommand(
                "  android-0123456789abcdef  ",
                "  CASE-2026-910  ",
                "  sha  ",
                DateTimeOffset.FromUnixTimeMilliseconds(1775156400000),
                [
                    new TextCapturePayload("  com.whatsapp  ", "  android.widget.TextView  ", "  Message content  ", null),
                    new TextCapturePayload("com.signal", "android.widget.TextView", null, "  Content description  ")
                ]),
            CancellationToken.None);

        result.Should().Be(IngestTextCaptureOutcome.Success);
        batchRepository.AddedBatches.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var batch = batchRepository.AddedBatches.Single();
        batch.CaseExternalId.Should().Be("CASE-2026-910");
        batch.DeviceId.Should().Be("android-0123456789abcdef");
        batch.CaptureCount.Should().Be(2);

        using var payload = JsonDocument.Parse(batch.PayloadJson);
        payload.RootElement.GetArrayLength().Should().Be(2);
        payload.RootElement[0].GetProperty("PackageName").GetString().Should().Be("com.whatsapp");
        payload.RootElement[0].GetProperty("Text").GetString().Should().Be("Message content");

        auditRepository.AddedEntries.Should().ContainSingle();
        using var auditPayload = JsonDocument.Parse(auditRepository.AddedEntries.Single().PayloadJson!);
        auditPayload.RootElement.GetProperty("CaseId").GetString().Should().Be("CASE-2026-910");
        auditPayload.RootElement.GetProperty("DeviceId").GetString().Should().Be("android-0123456789abcdef");
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public Guid? ExistingCaseId { get; set; }

        public Task AddAsync(Domain.Cases.Case entity, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Contracts.Cases.CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(externalCaseId == "CASE-2026-910" ? ExistingCaseId : null);
        }
    }

    private sealed class FakeDeviceSourceRepository : IDeviceSourceRepository
    {
        public DeviceSource? ExistingSource { get; set; }

        public Task AddAsync(DeviceSource entity, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DeviceSource?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingSource is not null && ExistingSource.DeviceId == deviceId ? ExistingSource : null);
        }
    }

    private sealed class FakeTextCaptureBatchRepository : ITextCaptureBatchRepository
    {
        public List<TextCaptureBatch> AddedBatches { get; } = [];

        public Task AddAsync(TextCaptureBatch entity, CancellationToken cancellationToken)
        {
            AddedBatches.Add(entity);
            return Task.CompletedTask;
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

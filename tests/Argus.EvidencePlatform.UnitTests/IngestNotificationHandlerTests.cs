using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Notifications.IngestNotification;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Domain.Notifications;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IngestNotificationHandlerTests
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
                "CASE-OTHER-001",
                new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero))
        };
        var handler = new IngestNotificationHandler(
            caseRepository,
            deviceRepository,
            new FakeNotificationCaptureRepository(),
            new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new IngestNotificationCommand(
                "android-0123456789abcdef",
                "CASE-2026-900",
                "sha",
                DateTimeOffset.FromUnixTimeMilliseconds(1775156400000),
                "com.whatsapp",
                "Sender",
                "Message preview",
                null,
                DateTimeOffset.FromUnixTimeMilliseconds(1775156400000),
                "msg"),
            CancellationToken.None);

        result.Should().Be(IngestNotificationOutcome.Conflict);
    }

    [Fact]
    public async Task Handle_should_persist_notification_and_write_audit_entry()
    {
        var caseId = Guid.NewGuid();
        var notificationRepository = new FakeNotificationCaptureRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new IngestNotificationHandler(
            new FakeCaseRepository { ExistingCaseId = caseId },
            new FakeDeviceSourceRepository
            {
                ExistingSource = DeviceSource.Register(
                    Guid.NewGuid(),
                    "android-0123456789abcdef",
                    caseId,
                    "CASE-2026-900",
                    new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero))
            },
            notificationRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new IngestNotificationCommand(
                "  android-0123456789abcdef  ",
                "  CASE-2026-900  ",
                "  sha  ",
                DateTimeOffset.FromUnixTimeMilliseconds(1775156400000),
                "  com.whatsapp  ",
                "  Sender  ",
                "  Message preview  ",
                "  Expanded message preview  ",
                DateTimeOffset.FromUnixTimeMilliseconds(1775156405000),
                "  msg  "),
            CancellationToken.None);

        result.Should().Be(IngestNotificationOutcome.Success);
        notificationRepository.AddedCaptures.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var notification = notificationRepository.AddedCaptures.Single();
        notification.CaseExternalId.Should().Be("CASE-2026-900");
        notification.DeviceId.Should().Be("android-0123456789abcdef");
        notification.PackageName.Should().Be("com.whatsapp");
        notification.Title.Should().Be("Sender");
        notification.Text.Should().Be("Message preview");
        notification.BigText.Should().Be("Expanded message preview");
        notification.Category.Should().Be("msg");

        auditRepository.AddedEntries.Should().ContainSingle();
        using var payload = JsonDocument.Parse(auditRepository.AddedEntries.Single().PayloadJson!);
        payload.RootElement.GetProperty("CaseId").GetString().Should().Be("CASE-2026-900");
        payload.RootElement.GetProperty("DeviceId").GetString().Should().Be("android-0123456789abcdef");
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
            return Task.FromResult(externalCaseId == "CASE-2026-900" ? ExistingCaseId : null);
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

    private sealed class FakeNotificationCaptureRepository : INotificationCaptureRepository
    {
        public List<NotificationCapture> AddedCaptures { get; } = [];

        public Task AddAsync(NotificationCapture entity, CancellationToken cancellationToken)
        {
            AddedCaptures.Add(entity);
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

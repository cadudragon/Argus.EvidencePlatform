using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Enrollment.ActivateDevice;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Domain.Enrollment;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class ActivateDeviceHandlerTests
{
    [Fact]
    public async Task Handle_should_return_not_found_when_token_does_not_exist()
    {
        var handler = new ActivateDeviceHandler(
            new FakeActivationTokenRepository(),
            new FakeDeviceSourceRepository(),
            new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ActivateDeviceCommand("123456789", "android-0123456789abcdef"),
            CancellationToken.None);

        result.Outcome.Should().Be(ActivateDeviceOutcome.NotFound);
    }

    [Fact]
    public async Task Handle_should_return_gone_when_token_is_expired()
    {
        var activationToken = ActivationToken.Issue(
            Guid.NewGuid(),
            "123456789",
            Guid.NewGuid(),
            "CASE-2026-402",
            new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero));
        var tokenRepository = new FakeActivationTokenRepository { ExistingToken = activationToken };
        var handler = new ActivateDeviceHandler(
            tokenRepository,
            new FakeDeviceSourceRepository(),
            new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new ActivateDeviceCommand("123456789", "android-0123456789abcdef"),
            CancellationToken.None);

        result.Outcome.Should().Be(ActivateDeviceOutcome.Gone);
    }

    [Fact]
    public async Task Handle_should_consume_token_register_device_and_write_audit_entry()
    {
        var caseId = Guid.NewGuid();
        var validUntil = new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero);
        var activationToken = ActivationToken.Issue(
            Guid.NewGuid(),
            "123456789",
            caseId,
            "CASE-2026-402",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            validUntil);
        var tokenRepository = new FakeActivationTokenRepository { ExistingToken = activationToken };
        var deviceSourceRepository = new FakeDeviceSourceRepository();
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero));
        var handler = new ActivateDeviceHandler(
            tokenRepository,
            deviceSourceRepository,
            auditRepository,
            clock,
            unitOfWork);

        var result = await handler.Handle(
            new ActivateDeviceCommand("123456789", "  android-0123456789abcdef  "),
            CancellationToken.None);

        result.Outcome.Should().Be(ActivateDeviceOutcome.Success);
        result.Response.Should().NotBeNull();
        result.Response!.CaseId.Should().Be("CASE-2026-402");
        result.Response.ValidUntil.Should().Be(validUntil.ToUnixTimeMilliseconds());
        result.Response.Scope.Should().BeEquivalentTo(["screenshot", "notification", "text"]);
        activationToken.IsConsumed.Should().BeTrue();
        deviceSourceRepository.AddedSources.Should().ContainSingle();
        auditRepository.AddedEntries.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);

        var auditEntry = auditRepository.AddedEntries.Single();
        auditEntry.CaseId.Should().Be(caseId);
        auditEntry.ActorType.Should().Be(AuditActorType.Device);

        using var payload = JsonDocument.Parse(auditEntry.PayloadJson!);
        payload.RootElement.GetProperty("CaseExternalId").GetString().Should().Be("CASE-2026-402");
    }

    private sealed class FakeActivationTokenRepository : IActivationTokenRepository
    {
        public ActivationToken? ExistingToken { get; set; }

        public Task AddAsync(ActivationToken entity, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ActivationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingToken is not null && ExistingToken.Token == token ? ExistingToken : null);
        }
    }

    private sealed class FakeDeviceSourceRepository : IDeviceSourceRepository
    {
        public List<DeviceSource> AddedSources { get; } = [];
        public DeviceSource? ExistingSource { get; set; }

        public Task AddAsync(DeviceSource entity, CancellationToken cancellationToken)
        {
            AddedSources.Add(entity);
            return Task.CompletedTask;
        }

        public Task<DeviceSource?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingSource is not null && ExistingSource.DeviceId == deviceId ? ExistingSource : null);
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

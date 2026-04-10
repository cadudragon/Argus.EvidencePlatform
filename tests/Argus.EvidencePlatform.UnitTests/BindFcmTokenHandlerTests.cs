using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Device.BindFcmToken;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class BindFcmTokenHandlerTests
{
    [Fact]
    public async Task Handle_should_return_gone_when_device_is_unknown()
    {
        var handler = new BindFcmTokenHandler(
            new FakeDeviceSourceRepository(),
            new FakeFirebaseAppRoutingResolver(),
            new FakeFcmTokenBindingRepository(),
            new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new BindFcmTokenCommand(
                "android-0123456789abcdef",
                "fcm-token",
                new FcmCommandKeyInput("ECDH-P256", "device-key", "public-key")),
            CancellationToken.None);

        result.Should().Be(BindFcmTokenOutcome.Gone);
    }

    [Fact]
    public async Task Handle_should_bind_token_for_active_device()
    {
        var deviceSource = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-403",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero));
        var deviceSourceRepository = new FakeDeviceSourceRepository { ExistingSource = deviceSource };
        var bindingRepository = new FakeFcmTokenBindingRepository();
        var routingResolver = new FakeFirebaseAppRoutingResolver
        {
            ExistingRouting = new FirebaseAppRoutingContext(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "fb-local-primary",
                "argus-local-primary")
        };
        var auditRepository = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new BindFcmTokenHandler(
            deviceSourceRepository,
            routingResolver,
            bindingRepository,
            auditRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        using var keys = new FcmCommandTestKeys();
        var result = await handler.Handle(
            new BindFcmTokenCommand(
                "android-0123456789abcdef",
                "fcm-token",
                new FcmCommandKeyInput("ECDH-P256", "device-key", keys.DevicePublicKey)),
            CancellationToken.None);

        result.Should().Be(BindFcmTokenOutcome.Success);
        bindingRepository.AddedBindings.Should().ContainSingle();
        bindingRepository.AddedBindings.Single().FcmCommandKeyKid.Should().Be("device-key");
        bindingRepository.AddedBindings.Single().FcmCommandKeyPublicKey.Should().Be(keys.DevicePublicKey);
        auditRepository.AddedEntries.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    private sealed class FakeFirebaseAppRoutingResolver : IFirebaseAppRoutingResolver
    {
        public FirebaseAppRoutingContext? ExistingRouting { get; set; }

        public Task<FirebaseAppRoutingContext?> ResolveForCaseAsync(Guid caseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingRouting);
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

    private sealed class FakeFcmTokenBindingRepository : IFcmTokenBindingRepository
    {
        public List<FcmTokenBinding> AddedBindings { get; } = [];
        public FcmTokenBinding? ExistingBinding { get; set; }

        public Task AddAsync(FcmTokenBinding entity, CancellationToken cancellationToken)
        {
            AddedBindings.Add(entity);
            return Task.CompletedTask;
        }

        public Task<FcmTokenBinding?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingBinding is not null && ExistingBinding.DeviceId == deviceId ? ExistingBinding : null);
        }

        public Task RemoveAsync(FcmTokenBinding entity, CancellationToken cancellationToken)
        {
            if (ExistingBinding == entity)
            {
                ExistingBinding = null;
            }

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

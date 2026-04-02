using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Device.RecordPong;
using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class RecordPongHandlerTests
{
    [Fact]
    public async Task Handle_should_return_gone_when_device_is_unknown()
    {
        var handler = new RecordPongHandler(
            new FakeDeviceSourceRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());

        var result = await handler.Handle(new RecordPongCommand("android-0123456789abcdef"), CancellationToken.None);

        result.Should().Be(RecordPongOutcome.Gone);
    }

    [Fact]
    public async Task Handle_should_record_last_seen_for_active_device()
    {
        var source = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-404",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero));
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero));
        var handler = new RecordPongHandler(
            new FakeDeviceSourceRepository { ExistingSource = source },
            clock,
            unitOfWork);

        var result = await handler.Handle(new RecordPongCommand("android-0123456789abcdef"), CancellationToken.None);

        result.Should().Be(RecordPongOutcome.Success);
        source.LastSeenAt.Should().Be(clock.UtcNow);
        unitOfWork.SaveChangesCalls.Should().Be(1);
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

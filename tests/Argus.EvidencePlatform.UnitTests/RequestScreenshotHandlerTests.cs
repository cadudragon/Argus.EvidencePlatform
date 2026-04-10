using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Device.RequestScreenshot;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class RequestScreenshotHandlerTests
{
    [Fact]
    public async Task Handle_should_return_not_found_when_device_is_unknown()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(
            new RequestScreenshotCommand("android-missing"),
            CancellationToken.None);

        result.Outcome.Should().Be(RequestScreenshotOutcome.NotFound);
    }

    [Fact]
    public async Task Handle_should_return_success_and_audit_when_dispatch_succeeds()
    {
        var firebaseAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var deviceSource = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-801",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero));
        var deviceRepo = new FakeDeviceSourceRepository { ExistingSource = deviceSource };
        var routingResolver = new FakeFirebaseAppRoutingResolver
        {
            ExistingRouting = new FirebaseAppRoutingContext(firebaseAppId, "fb-local-primary", "argus-local-primary")
        };
        var binding = FcmTokenBinding.Bind(
            Guid.NewGuid(),
            firebaseAppId,
            deviceSource.DeviceId,
            "fcm-token",
            "ECDH-P256",
            "device-key-001",
            "public-key-001",
            new DateTimeOffset(2026, 4, 2, 9, 30, 0, TimeSpan.Zero));
        var bindingRepo = new FakeFcmTokenBindingRepository { ExistingBinding = binding };
        var dispatcher = new FakeDeviceCommandDispatcher
        {
            Result = new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.Success, "firebase-message-01")
        };
        var auditRepo = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = BuildHandler(
            deviceRepo,
            routingResolver,
            bindingRepo,
            dispatcher: dispatcher,
            auditRepository: auditRepo,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new RequestScreenshotCommand(deviceSource.DeviceId),
            CancellationToken.None);

        result.Outcome.Should().Be(RequestScreenshotOutcome.Success);
        result.Response.Should().NotBeNull();
        result.Response!.MessageId.Should().Be("firebase-message-01");
        dispatcher.Requests.Should().ContainSingle();
        dispatcher.Requests.Single().Command.Should().Be("screenshot");
        dispatcher.Requests.Single().DeviceCommandKey.Kid.Should().Be("device-key-001");
        dispatcher.Requests.Single().Nonce.Should().Be("nonce-001");
        auditRepo.AddedEntries.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_remove_binding_when_firebase_rejects_token()
    {
        var firebaseAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var deviceSource = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-802",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero));
        var deviceRepo = new FakeDeviceSourceRepository { ExistingSource = deviceSource };
        var routingResolver = new FakeFirebaseAppRoutingResolver
        {
            ExistingRouting = new FirebaseAppRoutingContext(firebaseAppId, "fb-local-primary", "argus-local-primary")
        };
        var binding = FcmTokenBinding.Bind(
            Guid.NewGuid(),
            firebaseAppId,
            deviceSource.DeviceId,
            "fcm-token",
            "ECDH-P256",
            "device-key-001",
            "public-key-001",
            new DateTimeOffset(2026, 4, 2, 9, 30, 0, TimeSpan.Zero));
        var bindingRepo = new FakeFcmTokenBindingRepository { ExistingBinding = binding };
        var dispatcher = new FakeDeviceCommandDispatcher
        {
            Result = new DeviceCommandDispatchResult(DeviceCommandDispatchStatus.TokenInvalid, null)
        };
        var auditRepo = new FakeAuditRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = BuildHandler(
            deviceRepo,
            routingResolver,
            bindingRepo,
            dispatcher: dispatcher,
            auditRepository: auditRepo,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new RequestScreenshotCommand(deviceSource.DeviceId),
            CancellationToken.None);

        result.Outcome.Should().Be(RequestScreenshotOutcome.Gone);
        bindingRepo.RemovedBindings.Should().ContainSingle();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_should_return_conflict_when_binding_has_no_command_key()
    {
        var firebaseAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var deviceSource = DeviceSource.Register(
            Guid.NewGuid(),
            "android-0123456789abcdef",
            Guid.NewGuid(),
            "CASE-2026-803",
            new DateTimeOffset(2026, 4, 2, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero));
        var deviceRepo = new FakeDeviceSourceRepository { ExistingSource = deviceSource };
        var routingResolver = new FakeFirebaseAppRoutingResolver
        {
            ExistingRouting = new FirebaseAppRoutingContext(firebaseAppId, "fb-local-primary", "argus-local-primary")
        };
        var binding = FcmTokenBinding.Bind(
            Guid.NewGuid(),
            firebaseAppId,
            deviceSource.DeviceId,
            "fcm-token",
            "ECDH-P256",
            "device-key-001",
            "public-key-001",
            new DateTimeOffset(2026, 4, 2, 9, 30, 0, TimeSpan.Zero));
        typeof(FcmTokenBinding).GetProperty(nameof(FcmTokenBinding.FcmCommandKeyAlg))!.SetValue(binding, string.Empty);
        typeof(FcmTokenBinding).GetProperty(nameof(FcmTokenBinding.FcmCommandKeyKid))!.SetValue(binding, string.Empty);
        typeof(FcmTokenBinding).GetProperty(nameof(FcmTokenBinding.FcmCommandKeyPublicKey))!.SetValue(binding, string.Empty);
        var bindingRepo = new FakeFcmTokenBindingRepository { ExistingBinding = binding };
        var dispatcher = new FakeDeviceCommandDispatcher();

        var handler = BuildHandler(
            deviceRepo,
            routingResolver,
            bindingRepo,
            dispatcher: dispatcher);

        var result = await handler.Handle(
            new RequestScreenshotCommand(deviceSource.DeviceId),
            CancellationToken.None);

        result.Outcome.Should().Be(RequestScreenshotOutcome.Conflict);
        dispatcher.Requests.Should().BeEmpty();
    }

    private static RequestScreenshotHandler BuildHandler(
        FakeDeviceSourceRepository? deviceSourceRepository = null,
        FakeFirebaseAppRoutingResolver? routingResolver = null,
        FakeFcmTokenBindingRepository? fcmTokenBindingRepository = null,
        FakeCaseCommandPolicyRepository? caseCommandPolicyRepository = null,
        FakeDeviceCommandDispatcher? dispatcher = null,
        FakeAuditRepository? auditRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new RequestScreenshotHandler(
            deviceSourceRepository ?? new FakeDeviceSourceRepository(),
            routingResolver ?? new FakeFirebaseAppRoutingResolver(),
            fcmTokenBindingRepository ?? new FakeFcmTokenBindingRepository(),
            caseCommandPolicyRepository ?? new FakeCaseCommandPolicyRepository(),
            dispatcher ?? new FakeDeviceCommandDispatcher(),
            new FakeCommandNonceGenerator(),
            auditRepository ?? new FakeAuditRepository(),
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork ?? new FakeUnitOfWork());
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
        public FcmTokenBinding? ExistingBinding { get; set; }
        public List<FcmTokenBinding> RemovedBindings { get; } = [];

        public Task AddAsync(FcmTokenBinding entity, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<FcmTokenBinding?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingBinding is not null && ExistingBinding.DeviceId == deviceId ? ExistingBinding : null);
        }

        public Task RemoveAsync(FcmTokenBinding entity, CancellationToken cancellationToken)
        {
            RemovedBindings.Add(entity);
            if (ExistingBinding == entity)
            {
                ExistingBinding = null;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeFirebaseAppRoutingResolver : IFirebaseAppRoutingResolver
    {
        public FirebaseAppRoutingContext? ExistingRouting { get; set; }

        public Task<FirebaseAppRoutingContext?> ResolveForCaseAsync(Guid caseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingRouting);
        }
    }

    private sealed class FakeDeviceCommandDispatcher : IDeviceCommandDispatcher
    {
        public DeviceCommandDispatchResult Result { get; set; } = new(DeviceCommandDispatchStatus.Success, "message-id");
        public List<DeviceCommandDispatchRequest> Requests { get; } = [];

        public Task<DeviceCommandDispatchResult> DispatchAsync(DeviceCommandDispatchRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeCaseCommandPolicyRepository : ICaseCommandPolicyRepository
    {
        public Task<Argus.EvidencePlatform.Domain.Cases.CaseCommandPolicy> GetOrCreateDefaultAsync(
            Guid caseId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Argus.EvidencePlatform.Domain.Cases.CaseCommandPolicy.CreateDefault(Guid.NewGuid(), caseId, now));
        }
    }

    private sealed class FakeCommandNonceGenerator : ICommandNonceGenerator
    {
        public string CreateNonce() => "nonce-001";
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

using Argus.EvidencePlatform.Application.ActivationTokens.IssueActivationToken;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Cases;
using Argus.EvidencePlatform.Domain.Enrollment;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class IssueActivationTokenHandlerTests
{
    [Fact]
    public async Task Handle_should_return_null_when_case_does_not_exist()
    {
        var caseRepository = new FakeCaseRepository();
        var tokenRepository = new FakeActivationTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new IssueActivationTokenHandler(
            caseRepository,
            tokenRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new IssueActivationTokenCommand("CASE-2026-401", "123456789", new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        result.Should().BeNull();
        tokenRepository.AddedTokens.Should().BeEmpty();
        unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_should_create_activation_token_for_existing_case()
    {
        var caseId = Guid.NewGuid();
        var caseRepository = new FakeCaseRepository
        {
            CaseIdsByExternalId =
            {
                ["CASE-2026-401"] = caseId
            }
        };
        var tokenRepository = new FakeActivationTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new IssueActivationTokenHandler(
            caseRepository,
            tokenRepository,
            new FakeClock(new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero)),
            unitOfWork);

        var result = await handler.Handle(
            new IssueActivationTokenCommand("  CASE-2026-401  ", "123456789", new DateTimeOffset(2026, 4, 2, 11, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.CaseId.Should().Be("CASE-2026-401");
        result.Token.Should().Be("123456789");
        tokenRepository.AddedTokens.Should().ContainSingle();
        tokenRepository.AddedTokens.Single().CaseId.Should().Be(caseId);
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public Dictionary<string, Guid> CaseIdsByExternalId { get; } = [];

        public Task AddAsync(Case entity, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(CaseIdsByExternalId.ContainsKey(externalCaseId));
        }

        public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            return Task.FromResult(CaseIdsByExternalId.TryGetValue(externalCaseId, out var caseId) ? (Guid?)caseId : null);
        }

        public Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeActivationTokenRepository : IActivationTokenRepository
    {
        public List<ActivationToken> AddedTokens { get; } = [];

        public Task AddAsync(ActivationToken entity, CancellationToken cancellationToken)
        {
            AddedTokens.Add(entity);
            return Task.CompletedTask;
        }

        public Task<ActivationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedTokens.SingleOrDefault(x => x.Token == token));
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

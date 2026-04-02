using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Exports.GetExportJob;
using Argus.EvidencePlatform.Contracts.Exports;
using Argus.EvidencePlatform.Domain.Exports;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class GetExportJobHandlerTests
{
    [Fact]
    public async Task Handle_should_return_export_job_when_repository_finds_it()
    {
        var exportJobId = Guid.NewGuid();
        var expected = new ExportJobResponse(
            exportJobId,
            Guid.NewGuid(),
            nameof(ExportJobStatus.Queued),
            "Local Operator",
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            null,
            null,
            null);
        var repository = new FakeExportJobRepository
        {
            Response = expected
        };
        var handler = new GetExportJobHandler(repository);
        var query = new GetExportJobQuery(exportJobId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        repository.LastRequestedExportJobId.Should().Be(exportJobId);
    }

    [Fact]
    public async Task Handle_should_return_null_when_repository_does_not_find_job()
    {
        var exportJobId = Guid.NewGuid();
        var repository = new FakeExportJobRepository();
        var handler = new GetExportJobHandler(repository);
        var query = new GetExportJobQuery(exportJobId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        repository.LastRequestedExportJobId.Should().Be(exportJobId);
    }

    private sealed class FakeExportJobRepository : IExportJobRepository
    {
        public ExportJobResponse? Response { get; set; }

        public Guid? LastRequestedExportJobId { get; private set; }

        public Task AddAsync(ExportJob entity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ExportJobResponse?> GetByIdAsync(Guid exportJobId, CancellationToken cancellationToken)
        {
            LastRequestedExportJobId = exportJobId;
            return Task.FromResult(Response);
        }
    }
}

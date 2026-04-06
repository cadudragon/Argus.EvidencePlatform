using Argus.EvidencePlatform.Domain.Evidence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class EvidenceBlobConfiguration : IEntityTypeConfiguration<EvidenceBlob>
{
    public void Configure(EntityTypeBuilder<EvidenceBlob> entity)
    {
        entity.ToTable("evidence_blobs");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ContainerName).HasMaxLength(64);
        entity.Property(x => x.BlobName).HasMaxLength(1024);
        entity.Property(x => x.BlobVersionId).HasMaxLength(256);
        entity.Property(x => x.ContentType).HasMaxLength(128);
        entity.Property(x => x.Sha256).HasMaxLength(128);
    }
}

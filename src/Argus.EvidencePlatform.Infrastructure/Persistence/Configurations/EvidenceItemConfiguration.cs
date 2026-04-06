using Argus.EvidencePlatform.Domain.Evidence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class EvidenceItemConfiguration : IEntityTypeConfiguration<EvidenceItem>
{
    public void Configure(EntityTypeBuilder<EvidenceItem> entity)
    {
        entity.ToTable("evidence_items");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.SourceId).HasMaxLength(128);
        entity.Property(x => x.EvidenceType).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Classification).HasMaxLength(128);
        entity.HasOne(x => x.Blob)
            .WithOne()
            .HasForeignKey<EvidenceBlob>(x => x.EvidenceItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

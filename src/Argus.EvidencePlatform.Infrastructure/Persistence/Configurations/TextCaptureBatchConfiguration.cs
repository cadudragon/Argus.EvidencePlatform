using Argus.EvidencePlatform.Domain.TextCaptures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class TextCaptureBatchConfiguration : IEntityTypeConfiguration<TextCaptureBatch>
{
    public void Configure(EntityTypeBuilder<TextCaptureBatch> entity)
    {
        entity.ToTable("text_capture_batches");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.CaseId, x.CaptureTimestamp });
        entity.Property(x => x.CaseExternalId).HasMaxLength(128);
        entity.Property(x => x.DeviceId).HasMaxLength(128);
        entity.Property(x => x.Sha256).HasMaxLength(128);
        entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
        entity.Property(x => x.PackageNamesJson).HasColumnType("jsonb");
    }
}

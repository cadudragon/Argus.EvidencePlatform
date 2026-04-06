using Argus.EvidencePlatform.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> entity)
    {
        entity.ToTable("audit_entries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.ActorId).HasMaxLength(128);
        entity.Property(x => x.Action).HasMaxLength(128);
        entity.Property(x => x.EntityType).HasMaxLength(128);
        entity.Property(x => x.CorrelationId).HasMaxLength(128);
        entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
    }
}

using Argus.EvidencePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> entity)
    {
        entity.ToTable("cases");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ExternalCaseId).IsUnique();
        entity.HasIndex(x => x.FirebaseAppId);
        entity.Property(x => x.ExternalCaseId).HasMaxLength(128);
        entity.Property(x => x.Title).HasMaxLength(256);
        entity.Property(x => x.Description).HasMaxLength(2048);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
    }
}

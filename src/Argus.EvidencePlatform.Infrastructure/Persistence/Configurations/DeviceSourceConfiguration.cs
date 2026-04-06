using Argus.EvidencePlatform.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class DeviceSourceConfiguration : IEntityTypeConfiguration<DeviceSource>
{
    public void Configure(EntityTypeBuilder<DeviceSource> entity)
    {
        entity.ToTable("device_sources");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.DeviceId).IsUnique();
        entity.Property(x => x.DeviceId).HasMaxLength(128);
        entity.Property(x => x.CaseExternalId).HasMaxLength(128);
    }
}

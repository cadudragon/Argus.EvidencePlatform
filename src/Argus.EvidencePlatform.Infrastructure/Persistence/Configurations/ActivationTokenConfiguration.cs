using Argus.EvidencePlatform.Domain.Enrollment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
{
    public void Configure(EntityTypeBuilder<ActivationToken> entity)
    {
        entity.ToTable("activation_tokens");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Token).IsUnique();
        entity.Property(x => x.Token).HasMaxLength(9);
        entity.Property(x => x.CaseExternalId).HasMaxLength(128);
        entity.Property(x => x.ConsumedByDeviceId).HasMaxLength(128);
    }
}

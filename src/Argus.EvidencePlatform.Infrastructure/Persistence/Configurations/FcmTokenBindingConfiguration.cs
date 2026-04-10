using Argus.EvidencePlatform.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class FcmTokenBindingConfiguration : IEntityTypeConfiguration<FcmTokenBinding>
{
    public void Configure(EntityTypeBuilder<FcmTokenBinding> entity)
    {
        entity.ToTable("fcm_token_bindings");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.DeviceId).IsUnique();
        entity.HasIndex(x => x.FirebaseAppId);
        entity.Property(x => x.DeviceId).HasMaxLength(128);
        entity.Property(x => x.FcmToken).HasMaxLength(4096);
        entity.Property(x => x.FcmCommandKeyAlg).HasMaxLength(32);
        entity.Property(x => x.FcmCommandKeyKid).HasMaxLength(128);
        entity.Property(x => x.FcmCommandKeyPublicKey).HasMaxLength(2048);
    }
}

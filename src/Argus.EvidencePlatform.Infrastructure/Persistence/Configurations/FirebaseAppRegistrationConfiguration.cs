using Argus.EvidencePlatform.Domain.Firebase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class FirebaseAppRegistrationConfiguration : IEntityTypeConfiguration<FirebaseAppRegistration>
{
    public void Configure(EntityTypeBuilder<FirebaseAppRegistration> entity)
    {
        entity.ToTable("firebase_app_registrations");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Key).IsUnique();
        entity.Property(x => x.Key).HasMaxLength(128);
        entity.Property(x => x.DisplayName).HasMaxLength(256);
        entity.Property(x => x.ProjectId).HasMaxLength(256);
        entity.Property(x => x.ServiceAccountPath).HasMaxLength(2048);
    }
}

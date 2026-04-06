using Argus.EvidencePlatform.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class NotificationCaptureConfiguration : IEntityTypeConfiguration<NotificationCapture>
{
    public void Configure(EntityTypeBuilder<NotificationCapture> entity)
    {
        entity.ToTable("notification_captures");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.CaseId, x.CaptureTimestamp });
        entity.Property(x => x.CaseExternalId).HasMaxLength(128);
        entity.Property(x => x.DeviceId).HasMaxLength(128);
        entity.Property(x => x.Sha256).HasMaxLength(128);
        entity.Property(x => x.PackageName).HasMaxLength(256);
        entity.Property(x => x.Title).HasMaxLength(512);
        entity.Property(x => x.Text).HasMaxLength(4096);
        entity.Property(x => x.BigText).HasMaxLength(16384);
        entity.Property(x => x.Category).HasMaxLength(128);
    }
}

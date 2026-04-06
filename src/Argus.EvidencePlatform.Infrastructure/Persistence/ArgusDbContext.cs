using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Cases;
using Argus.EvidencePlatform.Domain.Devices;
using Argus.EvidencePlatform.Domain.Evidence;
using Argus.EvidencePlatform.Domain.Firebase;
using Argus.EvidencePlatform.Domain.Notifications;
using Argus.EvidencePlatform.Domain.TextCaptures;
using Argus.EvidencePlatform.Domain.Enrollment;
using Argus.EvidencePlatform.Domain.Exports;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence;

public sealed class ArgusDbContext(DbContextOptions<ArgusDbContext> options) : DbContext(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<ActivationToken> ActivationTokens => Set<ActivationToken>();
    public DbSet<DeviceSource> DeviceSources => Set<DeviceSource>();
    public DbSet<FcmTokenBinding> FcmTokenBindings => Set<FcmTokenBinding>();
    public DbSet<FirebaseAppRegistration> FirebaseAppRegistrations => Set<FirebaseAppRegistration>();
    public DbSet<NotificationCapture> NotificationCaptures => Set<NotificationCapture>();
    public DbSet<TextCaptureBatch> TextCaptureBatches => Set<TextCaptureBatch>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<EvidenceBlob> EvidenceBlobs => Set<EvidenceBlob>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("argus");

        modelBuilder.Entity<Case>(entity =>
        {
            entity.ToTable("cases");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalCaseId).IsUnique();
            entity.HasIndex(x => x.FirebaseAppId);
            entity.Property(x => x.ExternalCaseId).HasMaxLength(128);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(2048);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<FirebaseAppRegistration>(entity =>
        {
            entity.ToTable("firebase_app_registrations");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(128);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.ProjectId).HasMaxLength(256);
            entity.Property(x => x.ServiceAccountPath).HasMaxLength(2048);
        });

        modelBuilder.Entity<ActivationToken>(entity =>
        {
            entity.ToTable("activation_tokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Token).IsUnique();
            entity.Property(x => x.Token).HasMaxLength(9);
            entity.Property(x => x.CaseExternalId).HasMaxLength(128);
            entity.Property(x => x.ConsumedByDeviceId).HasMaxLength(128);
        });

        modelBuilder.Entity<DeviceSource>(entity =>
        {
            entity.ToTable("device_sources");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DeviceId).IsUnique();
            entity.Property(x => x.DeviceId).HasMaxLength(128);
            entity.Property(x => x.CaseExternalId).HasMaxLength(128);
        });

        modelBuilder.Entity<FcmTokenBinding>(entity =>
        {
            entity.ToTable("fcm_token_bindings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DeviceId).IsUnique();
            entity.HasIndex(x => x.FirebaseAppId);
            entity.Property(x => x.DeviceId).HasMaxLength(128);
            entity.Property(x => x.FcmToken).HasMaxLength(4096);
        });

        modelBuilder.Entity<NotificationCapture>(entity =>
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
        });

        modelBuilder.Entity<TextCaptureBatch>(entity =>
        {
            entity.ToTable("text_capture_batches");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CaseId, x.CaptureTimestamp });
            entity.Property(x => x.CaseExternalId).HasMaxLength(128);
            entity.Property(x => x.DeviceId).HasMaxLength(128);
            entity.Property(x => x.Sha256).HasMaxLength(128);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.Property(x => x.PackageNamesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<EvidenceItem>(entity =>
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
        });

        modelBuilder.Entity<EvidenceBlob>(entity =>
        {
            entity.ToTable("evidence_blobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContainerName).HasMaxLength(64);
            entity.Property(x => x.BlobName).HasMaxLength(1024);
            entity.Property(x => x.BlobVersionId).HasMaxLength(256);
            entity.Property(x => x.ContentType).HasMaxLength(128);
            entity.Property(x => x.Sha256).HasMaxLength(128);
        });

        modelBuilder.Entity<ExportJob>(entity =>
        {
            entity.ToTable("export_jobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RequestedBy).HasMaxLength(128);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorType).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ActorId).HasMaxLength(128);
            entity.Property(x => x.Action).HasMaxLength(128);
            entity.Property(x => x.EntityType).HasMaxLength(128);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
        });
    }
}

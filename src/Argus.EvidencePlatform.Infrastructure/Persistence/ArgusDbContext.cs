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
    public DbSet<CaseCommandPolicy> CaseCommandPolicies => Set<CaseCommandPolicy>();
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArgusDbContext).Assembly);
    }
}

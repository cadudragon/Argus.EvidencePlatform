using Argus.EvidencePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Configurations;

public sealed class CaseCommandPolicyConfiguration : IEntityTypeConfiguration<CaseCommandPolicy>
{
    public void Configure(EntityTypeBuilder<CaseCommandPolicy> entity)
    {
        entity.ToTable("case_command_policies");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.CaseId).IsUnique();
    }
}

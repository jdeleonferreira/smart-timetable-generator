using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STG.Domain.Entities;

namespace STG.Infrastructure.Persistence.Configurations;

public sealed class SchedulingConfigConfiguration : IEntityTypeConfiguration<SchedulingConfig>
{
    public void Configure(EntityTypeBuilder<SchedulingConfig> b)
    {
        b.ToTable("SchedulingConfigs");
        b.HasKey(x => x.Id);

        b.HasIndex(x => x.SchoolYearId).IsUnique();

        b.Property(x => x.PrioritiesJson).HasMaxLength(2000);

        b.HasOne<SchoolYear>()
            .WithMany()
            .HasForeignKey(x => x.SchoolYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional CHECKs
        b.ToTable(t => t.HasCheckConstraint("CK_Sched_MaxPerTeacher", "(MaxPeriodsPerDayTeacher IS NULL OR (MaxPeriodsPerDayTeacher BETWEEN 1 AND 20))"));
        b.ToTable(t => t.HasCheckConstraint("CK_Sched_MaxPerGroup", "(MaxPeriodsPerDayGroup IS NULL OR (MaxPeriodsPerDayGroup BETWEEN 1 AND 20))"));
        b.ToTable(t => t.HasCheckConstraint("CK_Sched_MaxConsecutive", "(MaxConsecutiveSameSubject IS NULL OR (MaxConsecutiveSameSubject BETWEEN 1 AND 10))"));
    }
}
